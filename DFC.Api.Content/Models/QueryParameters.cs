using System;
using System.Collections.Generic;

namespace DFC.Api.Content.Models
{
    public class QueryParameters
    {
        public QueryParameters(string contentType, List<Guid?> ids)
        {
            ContentType = contentType;
            Ids = ids;
        }

        public string ContentType { get; }
        public List<Guid?> Ids { get; }
    }
}
