using DFC.ServiceTaxonomy.ApiFunction.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace DFC.Api.Content.Helpers
{
    public class JsonFormatHelper : IJsonFormatHelper
    {
        private readonly IOptionsMonitor<ContentTypeMapSettings> _settings;

        public JsonFormatHelper(IOptionsMonitor<ContentTypeMapSettings> settings)
        {
            _settings = settings;
        }

        public string CreateSingleRootObject(object input)
        {
            var objToReturn = new JObject();

            JObject neoJsonObj = JObject.Parse(input.ToString());

            foreach (var child in neoJsonObj["data"].Children())
            {
                objToReturn.Add(child);
            }

            objToReturn.Add(new JProperty("_links", neoJsonObj["_links"]));

            return objToReturn.ToString();
        }

        public string ReplaceNamespaces(object input)
        {
            var serializedJson = JsonConvert.SerializeObject(input);

            foreach (var key in _settings.CurrentValue.ReversedContentTypeMap.Keys)
            {
                serializedJson = serializedJson.Replace($"\"{key}\"", $"\"{_settings.CurrentValue.ReversedContentTypeMap[key]}\"");
            }

            return serializedJson;
        }
    }
}
