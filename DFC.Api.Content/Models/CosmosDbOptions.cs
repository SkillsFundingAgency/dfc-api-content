using System.Collections.Generic;

namespace DFC.Api.Content.Models
{
    public class CosmosDbOptions
    {
        public List<string>? Endpoints { get; } = new List<string>();
    }
}