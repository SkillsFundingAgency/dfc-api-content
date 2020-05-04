using System.Collections.Generic;
using System.Linq;

namespace DFC.ServiceTaxonomy.ApiFunction.Models
{
    public class ContentTypeMapSettings
    {
        public ContentTypeMapSettings()
        {
            ContentTypeMap = new Dictionary<string, string>();
        }

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
