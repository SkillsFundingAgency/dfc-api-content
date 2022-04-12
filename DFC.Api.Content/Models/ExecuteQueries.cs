using DFC.Api.Content.Enums;

namespace DFC.Api.Content.Models
{
    public class ExecuteQueries
    {
        public ExecuteQueries(
            ExecuteQuery[] queries,
            RequestType requestType,
            string contentType,
            string publishState)
        {
            Queries = queries;
            RequestType = requestType;
            ContentType = contentType;
            PublishState = publishState;
        }
        
        public string ContentType  { get; }

        public RequestType RequestType { get; }
        
        public string PublishState { get; }
        
        public ExecuteQuery[] Queries { get; }
    }
}