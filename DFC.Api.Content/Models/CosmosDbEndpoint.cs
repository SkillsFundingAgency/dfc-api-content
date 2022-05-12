using System.Diagnostics.CodeAnalysis;

namespace DFC.Api.Content.Models
{
    // ReSharper disable once ClassNeverInstantiated.Global
    [ExcludeFromCodeCoverage]
    public class CosmosDbEndpoint
    {
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public string? ConnectionString { get; set; }
        
        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public string? DatabaseName { get; set; }

        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public string? ContainerName { get; set; }
    }
}