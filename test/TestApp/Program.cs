using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using E;
using System.Net.Http;

namespace TestApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run(RunSuccessCallback);
        }


        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            return WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
        }


        /// <summary>
        /// 运行成功的回调函数
        /// </summary>
        public static void RunSuccessCallback()
        {
            var httpClient = new HttpClient();
            var responseMessage = httpClient.GetAsync("http://localhost:24253/api/values").GetAwaiter().GetResult();

            var result = responseMessage.Content.ReadAsStringAsync().GetAwaiter().GetResult();


            Console.WriteLine($"api reponse: {result}");
        }
    }
}
