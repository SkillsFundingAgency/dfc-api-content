namespace DFC.Api.Content.Models
{
    // ReSharper disable once ClassNeverInstantiated.Global
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