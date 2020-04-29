using System;

namespace DFC.ServiceTaxonomy.ApiFunction.Models
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
