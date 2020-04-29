using System;
using System.Collections.Generic;
using System.Text;

namespace DFC.ServiceTaxonomy.ApiFunction.Models
{
    public class ContentTypeMapSettings
    {
        public bool OverrideUri { get; set; }
        public Dictionary<string, string> Values { get; set; }
    }
}
