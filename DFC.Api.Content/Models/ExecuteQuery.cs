using DFC.Api.Content.Enums;

namespace DFC.Api.Content.Models
{
    public class ExecuteQuery
    {
        public ExecuteQuery(string queryText, RequestType requestType, string contentType)
        {
            QueryText = queryText;
            RequestType = requestType;
            ContentType = contentType;
        }

        public string QueryText { get; }
        
        public string ContentType  { get; }

        public RequestType RequestType { get; }
    }
}