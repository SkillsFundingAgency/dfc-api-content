using DFC.Api.Content.Helpers;
using DFC.ServiceTaxonomy.ApiFunction.Models;
using DFC.ServiceTaxonomy.ApiFunction.StartUp;
using DFC.ServiceTaxonomy.Neo4j.Configuration;
using DFC.ServiceTaxonomy.Neo4j.Log;
using DFC.ServiceTaxonomy.Neo4j.Services;
using DFC.ServiceTaxonomy.Neo4j.Services.Interfaces;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Neo4j.Driver;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;

[assembly: FunctionsStartup(typeof(FunctionStartupExtension))]

namespace DFC.ServiceTaxonomy.ApiFunction.StartUp
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

            builder.Services.AddOptions<ContentApiOptions>()
                .Configure<IConfiguration>((settings, configuration) => { configuration.GetSection("ContentApiOptions").Bind(settings); });

            builder.Services.AddOptions<Neo4jOptions>()
                .Configure<IConfiguration>((settings, configuration) => { configuration.GetSection("Neo4j").Bind(settings); });

            builder.Services.AddSingleton<IGraphClusterBuilder, GraphClusterBuilder>();
            builder.Services.AddTransient<ILogger, NeoLogger>();

            builder.Services.AddTransient<ILogger, NeoLogger>();
            builder.Services.AddSingleton<IJsonFormatHelper, JsonFormatHelper>();

        }

        private static string GetCustomSettingsPath()
        {
            var home = Environment.GetEnvironmentVariable("HOME");
            string? path;
            if (home != null)
            {
                // We're on Azure
                path = Path.Combine(home, "site", "wwwroot");
            }
            else
            {
                // Running locally
                path = new Uri(Assembly.GetExecutingAssembly().CodeBase!).LocalPath;

                if (!string.IsNullOrEmpty(path))
                {
                    path = Path.GetDirectoryName(path);
                    DirectoryInfo parentDir = Directory.GetParent(path);
                    path = parentDir.FullName;
                }
            }

            return path ?? throw new ServiceUnavailableException("Path for settings could not be determined");
        }
    }
}