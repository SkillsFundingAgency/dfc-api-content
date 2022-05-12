using DFC.Api.Content.Enums;

namespace DFC.Api.Content.Models
{
    public class Queries
    {
        public Queries(
            Query[] content,
            RequestType requestType,
            string contentType,
            string publishState)
        {
            Content = content;
            RequestType = requestType;
            ContentType = contentType;
            PublishState = publishState;
        }
        
        public string ContentType  { get; }

        public RequestType RequestType { get; }
        
        public string PublishState { get; }
        
        public Query[] Content { get; }
    }
}