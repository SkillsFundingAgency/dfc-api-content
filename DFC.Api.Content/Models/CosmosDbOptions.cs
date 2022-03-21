using System.Collections.Generic;

namespace DFC.Api.Content.Models
{
    public class CosmosDbOptions
    {
        public Dictionary<string, Dictionary<string, string>>? Endpoints { get; } = new Dictionary<string, Dictionary<string, string>>();
    }
}