using System;

namespace DFC.Api.Content.Models
{
    public class QueryParameters
    {
        public QueryParameters(string contentType, Guid? id)
        {
            ContentType = contentType;
            Id = id;
        }

        public string ContentType { get; set; }
        public Guid? Id { get; set; }
    }
}
