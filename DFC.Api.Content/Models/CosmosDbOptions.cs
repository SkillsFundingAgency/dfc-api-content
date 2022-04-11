using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace DFC.Api.Content.Models
{
    [ExcludeFromCodeCoverage]
    public class CosmosDbOptions
    {
        // ReSharper disable once CollectionNeverUpdated.Global
        public Dictionary<string, CosmosDbEndpoint>? Endpoints { get; } = new Dictionary<string, CosmosDbEndpoint>();
    }
}