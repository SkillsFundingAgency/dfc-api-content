using System.Collections.Generic;
using System.Linq;

namespace DFC.ServiceTaxonomy.ApiFunction.Models
{
    public class ContentTypeSettings
    {
        public ContentTypeSettings()
        {
            ContentTypeUriMap = new Dictionary<string, string>();
            ContentTypeNameMap = new Dictionary<string, string>();
        }

        public bool OverrideUri { get; set; }
        public Dictionary<string, string> ContentTypeUriMap { get; set; }
        public Dictionary<string, string> ContentTypeNameMap { get; set; }

        public Dictionary<string, string> ReversedContentTypeUriMap
        {
            get
            {
                return ContentTypeUriMap.ToDictionary(x => x.Value, x => x.Key);
            }
        }

        public Dictionary<string, string> ReversedContentTypeNameMap
        {
            get
            {
                return ContentTypeNameMap.ToDictionary(x => x.Value, x => x.Key);
            }
        }
    }
}
