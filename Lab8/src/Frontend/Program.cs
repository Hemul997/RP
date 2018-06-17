using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Frontend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) 
        {
            var mainDir = Directory.GetParent(Directory.GetCurrentDirectory());
            var configPath = mainDir + "/config";
            if (!Directory.Exists(configPath))
            {
                throw new DirectoryNotFoundException("Not found configuration file");
            }
            DirectoryInfo dirConfig = new DirectoryInfo(configPath);
                var config = new ConfigurationBuilder()
                .SetBasePath(dirConfig.FullName)
                .AddJsonFile("config_frontend.json")
                .Build();
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseConfiguration(config)
                .UseContentRoot(Directory.GetCurrentDirectory())                
                .UseStartup<Startup>()
                .Build();
            return host;
        }
    }
}
