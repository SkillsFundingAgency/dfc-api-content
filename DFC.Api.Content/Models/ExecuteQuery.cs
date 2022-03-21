using DFC.Api.Content.Enums;

namespace DFC.Api.Content.Models
{
    public class ExecuteQuery
    {
        public ExecuteQuery(string queryText, RequestType requestType, string contentType, string publishState)
        {
            QueryText = queryText;
            RequestType = requestType;
            ContentType = contentType;
            PublishState = publishState;
        }

        public string QueryText { get; }
        
        public string ContentType  { get; }

        public RequestType RequestType { get; }
        
        public string PublishState { get; }
    }
}