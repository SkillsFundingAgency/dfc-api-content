using System.Collections.Generic;

namespace DFC.Api.Content.Models
{
    public class CosmosDbOptions
    {
        // ReSharper disable once CollectionNeverUpdated.Global
        public Dictionary<string, CosmosDbEndpoint>? Endpoints { get; } = new Dictionary<string, CosmosDbEndpoint>();
    }
}