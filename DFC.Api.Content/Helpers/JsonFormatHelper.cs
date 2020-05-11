using DFC.Api.Content.Enums;
using DFC.ServiceTaxonomy.ApiFunction.Exceptions;
using DFC.ServiceTaxonomy.ApiFunction.Models;
using Microsoft.Extensions.Options;
using Neo4j.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DFC.Api.Content.Helpers
{
    public class JsonFormatHelper : IJsonFormatHelper
    {
        private readonly IOptionsMonitor<ContentTypeSettings> _settings;

        public JsonFormatHelper(IOptionsMonitor<ContentTypeSettings> settings)
        {
            _settings = settings;
        }

        public object FormatResponse(IEnumerable<IRecord> recordsResult, RequestType type)
        {
            switch (type)
            {
                case RequestType.GetAll:
                    return recordsResult.SelectMany(z => z.Values).Select(y => CreateSingleRootObject(ReplaceNamespaces(y.Value)));
                case RequestType.GetById:
                    var recordValues = recordsResult.Select(z => z.Values).FirstOrDefault()?.Values.FirstOrDefault();
                    if (recordValues != null)
                    {
                        return this.CreateSingleRootObject(this.ReplaceNamespaces(recordValues));
                    }

                    throw ApiFunctionException.InternalServerError($"Request Type: {type} records contain unformattable response");

                default:
                    throw new NotSupportedException($"Request Type: {type} not supported");
            }
        }

        private object CreateSingleRootObject(object input)
        {
            var objToReturn = new JObject();

            JObject neoJsonObj = JObject.Parse(input.ToString() ?? string.Empty);

            foreach (var child in neoJsonObj["data"]!.Children())
            {
                objToReturn.Add(child);
            }

            objToReturn.Add(new JProperty("_links", neoJsonObj["_links"]));

            return objToReturn;
        }

        private object ReplaceNamespaces(object input)
        {
            var serializedJson = JsonConvert.SerializeObject(input);

            foreach (var key in _settings.CurrentValue.ReversedContentTypeNameMap.Keys)
            {
                serializedJson = serializedJson.Replace($"\"{key}\"", $"\"{_settings.CurrentValue.ReversedContentTypeNameMap[key]}\"");
            }

            return JsonConvert.DeserializeObject<object>(serializedJson);
        }
    }
}
