using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using DFC.Api.Content;
using DFC.Api.Content.Helpers;
using DFC.Api.Content.Interfaces;
using DFC.Api.Content.Models;
using DFC.Api.Content.Services;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

[assembly: FunctionsStartup(typeof(FunctionStartupExtension))]
namespace DFC.Api.Content
{
    [ExcludeFromCodeCoverage]
    public class FunctionStartupExtension : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(GetCustomSettingsPath())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings.development.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            builder.Services.AddSingleton<IConfiguration>(config);

            builder.Services.AddOptions<CosmosDbOptions>()
                .Configure<IConfiguration>((settings, configuration) => { configuration.GetSection("CosmosDb").Bind(settings); });
            
            builder.Services.AddSingleton<IDataSourceProvider, CosmosDbService>(services =>
            {
                var options = services.GetRequiredService<IOptions<CosmosDbOptions>>();
                var preview = options.Value.Endpoints!["preview"];
                var published = options.Value.Endpoints!["published"];
                
                return new CosmosDbService(
                    preview.ConnectionString,
                    preview.DatabaseName,
                    preview.ContainerName,
                    published.ConnectionString,
                    published.DatabaseName,
                    published.ContainerName);
            });
            
            builder.Services.AddSingleton<IJsonFormatHelper, JsonFormatHelper>();
        }

        private static string GetCustomSettingsPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME") ?? string.Empty;
            var path = Path.Combine(home, "site", "wwwroot");

            if (Directory.Exists(path))
            {
                return path;
            }
            
            path = new Uri(Assembly.GetExecutingAssembly().CodeBase!).LocalPath;

            if (string.IsNullOrEmpty(path))
            {
                return path ?? throw new Exception("Path for settings could not be determined");
            }

            path = Path.GetDirectoryName(path);
            var parentDir = Directory.GetParent(path);
            path = parentDir.FullName;

            return path ?? throw new Exception("Path for settings could not be determined");
        }
    }
}