using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DFC.ServiceTaxonomy.ApiFunction.Models
{
    public class ContentTypeMapSettings
    {
        public bool OverrideUri { get; set; }
        public Dictionary<string, string> ContentTypeMap { get; set; }

        public Dictionary<string, string> ReversedContentTypeMap
        {
            get
            {
                return ContentTypeMap.ToDictionary(x => x.Value, x => x.Key);
            }
        }
    }
}
