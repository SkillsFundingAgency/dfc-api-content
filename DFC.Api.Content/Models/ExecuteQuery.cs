using DFC.Api.Content.Enums;

namespace DFC.Api.Content.Models
{
    public class ExecuteQuery
    {
        public ExecuteQuery(string queryText, RequestType requestType, string contentType, string state)
        {
            QueryText = queryText;
            RequestType = requestType;
            ContentType = contentType;
            State = state;
        }

        public string QueryText { get; }
        
        public string ContentType  { get; }

        public RequestType RequestType { get; }
        
        public string State { get; }
    }
}