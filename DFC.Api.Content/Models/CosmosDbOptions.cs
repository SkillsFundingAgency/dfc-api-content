using System.Collections.Generic;

namespace DFC.Api.Content.Models
{
    public class CosmosDbOptions
    {
        public Dictionary<string, CosmosDbEndpoint>? Endpoints { get; } = new Dictionary<string, CosmosDbEndpoint>();
    }
}