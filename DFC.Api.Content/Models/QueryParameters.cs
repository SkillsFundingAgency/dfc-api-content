using System;

namespace DFC.ServiceTaxonomy.ApiFunction.Models
{
    public class QueryParameters
    {
        public string ContentType { get; set; }
        public Guid? Id { get; set; }

        public string RequestPath { get; set; }
    }
}
