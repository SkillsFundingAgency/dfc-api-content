using System.Collections.Generic;
using System.Linq;

namespace DFC.ServiceTaxonomy.ApiFunction.Models
{
    public class ContentApiOptions
    {
        public ContentApiOptions()
        {
            ContentTypeUriMap = new Dictionary<string, string>();
            ContentTypeNameMap = new Dictionary<string, string>();
        }

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

        public string? Scheme { get; set; }

        public string? Action { get; set; }
    }
}
