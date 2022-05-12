using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace DFC.Api.Content.Models
{
    [ExcludeFromCodeCoverage]
    public class GenericQuery
    {
        public GenericQuery(string queryText, string contentType, string publishState, Dictionary<string, object> parameters)
        {
            QueryText = queryText;
            ContentType = contentType;
            PublishState = publishState;
            Parameters = parameters;
        }
        
        public string QueryText { get; }

        public string ContentType { get; }
        
        public string PublishState { get; }
        
        public Dictionary<string, object> Parameters { get; }
    }
}
