namespace DFC.Api.Content.Models
{
    public class CosmosDbEndpoint
    {
        public string? ConnectionString { get; set; }
        public string? DatabaseName { get; set; }
        public string? ContainerName { get; set; }
    }
}