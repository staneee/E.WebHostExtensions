using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;

namespace E
{
    public static class EWebHostExtensions
    {

        /// <summary>
        /// Attempts to gracefully stop the host with the given timeout.
        /// </summary>
        /// <param name="host"></param>
        /// <param name="timeout">The timeout for stopping gracefully. Once expired the
        /// server may terminate any remaining active connections.</param>
        /// <returns></returns>
        public static Task StopAsync(this IWebHost host, TimeSpan timeout)
        {
            return host.StopAsync(new CancellationTokenSource(timeout).Token);
        }

        /// <summary>
        /// Block the calling thread until shutdown is triggered via Ctrl+C or SIGTERM.
        /// </summary>
        /// <param name="host">The running <see cref="T:Microsoft.AspNetCore.Hosting.IWebHost" />.</param>
        /// <param name="runSuccessCallback">启动成功的回调函数(Run the successful callback)</param>
        public static void WaitForShutdown(this IWebHost host, Action runSuccessCallback = null)
        {
            host.WaitForShutdownAsync(new CancellationToken(), runSuccessCallback).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Returns a Task that completes when shutdown is triggered via the given token, Ctrl+C or SIGTERM.
        /// </summary>
        /// <param name="host">The running <see cref="T:Microsoft.AspNetCore.Hosting.IWebHost" />.</param>
        /// <param name="token">The token to trigger shutdown.</param>
        /// <param name="runSuccessCallback">启动成功的回调函数(Run the successful callback)</param>
        public static async Task WaitForShutdownAsync(this IWebHost host, CancellationToken token = default(CancellationToken), Action runSuccessCallback = null)
        {
            ManualResetEventSlim done = new ManualResetEventSlim(false);
            using (CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(token))
            {
                EWebHostExtensions.AttachCtrlcSigtermShutdown(cts, done, string.Empty);
                await host.WaitForTokenShutdownAsync(cts.Token, runSuccessCallback);
                done.Set();
            }
        }

        /// <summary>
        /// Runs a web application and block the calling thread until host shutdown.
        /// </summary>
        /// <param name="host">The <see cref="T:Microsoft.AspNetCore.Hosting.IWebHost" /> to run.</param>
        /// <param name="runSuccessCallback">启动成功的回调函数(Run the successful callback)</param>
        public static void Run(this IWebHost host, Action runSuccessCallback = null)
        {
            host.RunAsync(new CancellationToken(), runSuccessCallback).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Runs a web application and returns a Task that only completes when the token is triggered or shutdown is triggered.
        /// </summary>
        /// <param name="host">The <see cref="T:Microsoft.AspNetCore.Hosting.IWebHost" /> to run.</param>
        /// <param name="token">The token to trigger shutdown.</param>
        /// <param name="runSuccessCallback">启动成功的回调函数(Run the successful callback)</param>
        public static async Task RunAsync(this IWebHost host, CancellationToken token = default(CancellationToken), Action runSuccessCallback = null)
        {
            if (token.CanBeCanceled)
            {
                await host.RunAsync(token, (string)null, runSuccessCallback);
            }
            else
            {
                ManualResetEventSlim done = new ManualResetEventSlim(false);
                using (CancellationTokenSource cts = new CancellationTokenSource())
                {
                    EWebHostExtensions.AttachCtrlcSigtermShutdown(cts, done, host.Services.GetRequiredService<WebHostOptions>().SuppressStatusMessages ? string.Empty : "Application is shutting down...");

                    await host.RunAsync(
                        cts.Token,
                        "Application started. Press Ctrl+C to shut down.",
                        runSuccessCallback);

                    done.Set();
                }
            }
        }

        private static async Task RunAsync(
          this IWebHost host,
          CancellationToken token,
          string shutdownMessage,
          Action runSuccessCallback)
        {
            using (host)
            {
                await host.StartAsync(token);
                IHostingEnvironment service = host.Services.GetService<IHostingEnvironment>();
                host.Services.GetService<IApplicationLifetime>();
                if (!host.Services.GetRequiredService<WebHostOptions>().SuppressStatusMessages)
                {
                    Console.WriteLine(string.Format("Hosting environment: {0}", (object)service.EnvironmentName));
                    Console.WriteLine(string.Format("Content root path: {0}", (object)service.ContentRootPath));
                    ICollection<string> addresses = host.ServerFeatures.Get<IServerAddressesFeature>()?.Addresses;
                    if (addresses != null)
                    {
                        foreach (object obj in (IEnumerable<string>)addresses)
                            Console.WriteLine(string.Format("Now listening on: {0}", obj));
                    }
                    if (!string.IsNullOrEmpty(shutdownMessage))
                        Console.WriteLine(shutdownMessage);
                }

                await host.WaitForTokenShutdownAsync(token, runSuccessCallback);
            }
        }

        private static void AttachCtrlcSigtermShutdown(
          CancellationTokenSource cts,
          ManualResetEventSlim resetEvent,
          string shutdownMessage)
        {
            AppDomain.CurrentDomain.ProcessExit += (EventHandler)((sender, eventArgs) => Shutdown());
            Console.CancelKeyPress += (ConsoleCancelEventHandler)((sender, eventArgs) =>
            {
                Shutdown();
                eventArgs.Cancel = true;
            });

            void Shutdown()
            {
                if (!cts.IsCancellationRequested)
                {
                    if (!string.IsNullOrEmpty(shutdownMessage))
                        Console.WriteLine(shutdownMessage);
                    try
                    {
                        cts.Cancel();
                    }
                    catch (ObjectDisposedException ex)
                    {
                    }
                }
                resetEvent.Wait();
            }
        }

        private static async Task WaitForTokenShutdownAsync(
          this IWebHost host,
          CancellationToken token,
          Action runSuccessCallback)
        {
            IApplicationLifetime service = host.Services.GetService<IApplicationLifetime>();
            token.Register((Action<object>)(state => ((IApplicationLifetime)state).StopApplication()), (object)service);
            TaskCompletionSource<object> completionSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            service.ApplicationStopping.Register((Action<object>)(obj => ((TaskCompletionSource<object>)obj).TrySetResult((object)null)), (object)completionSource);

            // 触发启动成功回调事件
            runSuccessCallback?.Invoke();

            object task = await completionSource.Task;
            await host.StopAsync(new CancellationToken());
        }
    }
}
