using DFC.Api.Content.Helpers;
using DFC.ServiceTaxonomy.ApiFunction.Models;
using DFC.ServiceTaxonomy.ApiFunction.StartUp;
using DFC.ServiceTaxonomy.Neo4j.Configuration;
using DFC.ServiceTaxonomy.Neo4j.Log;
using DFC.ServiceTaxonomy.Neo4j.Services;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Neo4j.Driver;
using System.Diagnostics.CodeAnalysis;
using System.IO;

[assembly: FunctionsStartup(typeof(FunctionStartupExtension))]

namespace DFC.ServiceTaxonomy.ApiFunction.StartUp
{
    [ExcludeFromCodeCoverage]
    public class FunctionStartupExtension : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings.development.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            builder.Services.AddSingleton<IConfiguration>(config);

            builder.Services.AddOptions<ContentTypeSettings>()
                .Configure<IConfiguration>((settings, configuration) => { configuration.GetSection("ContentType").Bind(settings); });

            builder.Services.AddOptions<Neo4jConfiguration>()
                .Configure<IConfiguration>((settings, configuration) => { configuration.GetSection("Neo4j").Bind(settings); });

            builder.Services.AddTransient<ILogger, NeoLogger>();
            builder.Services.AddSingleton<INeoDriverBuilder, NeoDriverBuilder>();
            builder.Services.AddSingleton<IGraphDatabase, NeoGraphDatabase>();
            builder.Services.AddSingleton<IJsonFormatHelper, JsonFormatHelper>();

        }
    }
}
