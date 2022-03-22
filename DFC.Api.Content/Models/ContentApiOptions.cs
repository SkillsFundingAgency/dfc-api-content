using System.Collections.Generic;

namespace DFC.Api.Content.Models
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
        
        public string? Scheme { get; set; }

        public string? Action { get; set; }
    }
}
