using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;

namespace Tailspin.Surveys.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)

                // Uncomment the block of code below if you want to load secrets from KeyVault
                // It is recommended to use certs for all authentication when using KeyVault
                //.ConfigureAppConfiguration((context, config) =>
                //{
                //    var builtConfig = config.Build();
                //
                //    if (context.HostingEnvironment.IsProduction())
                //    {
                //        config.AddAzureKeyVault(
                //            $"https://{builtConfig["KeyVault:Name"]}.vault.azure.net/"
                //            , builtConfig["AzureAd:ClientId"]
                //            , builtConfig["AzureAd:ClientSecret"]);
                //    }
                //})
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}

