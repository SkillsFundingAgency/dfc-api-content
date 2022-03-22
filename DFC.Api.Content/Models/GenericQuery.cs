using System.Diagnostics.CodeAnalysis;

namespace DFC.Api.Content.Models
{
    [ExcludeFromCodeCoverage]
    public class GenericQuery
    {
        public GenericQuery(string queryText, string contentType, string publishState)
        {
            QueryText = queryText;
            ContentType = contentType;
            PublishState = publishState;
        }
        
        public string QueryText { get; }
        public string ContentType { get; }
        
        public string PublishState { get; }
    }
}
