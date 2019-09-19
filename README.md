# E.WebHostExtensions


## 功能说明

* 扩展 IWebHost, Run 函数增加启动成功后的回调

**使用方法**

```c#
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
```


## 注意

本库目前只在 aspnet core 2.1.1 上测试 , 若要使用其他版本的请提 issue 或 pr
