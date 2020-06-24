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

        public object FormatResponse(IEnumerable<IRecord> recordsResult, RequestType type, string apiHost)
        {
            switch (type)
            {
                case RequestType.GetAll:
                    return recordsResult.SelectMany(z => z.Values).Select(y => CreateSingleRootObject(ReplaceNamespaces(y.Value), apiHost, false));
                case RequestType.GetById:
                    var recordValues = recordsResult.Select(z => z.Values).FirstOrDefault()?.Values.FirstOrDefault();
                    if (recordValues != null)
                    {
                        return this.CreateSingleRootObject(this.ReplaceNamespaces(recordValues), apiHost, true);
                    }

                    throw ApiFunctionException.InternalServerError($"Request Type: {type} records contain unformattable response");

                default:
                    throw new NotSupportedException($"Request Type: {type} not supported");
            }
        }

        private object CreateSingleRootObject(object input, string apiHost, bool includeLinks)
        {
            var objToReturn = new JObject();

            JObject neoJsonObj = JObject.Parse(input.ToString() ?? string.Empty);

            foreach (var child in neoJsonObj["data"]!.Children())
            {
                objToReturn.Add(child);
            }

            if (includeLinks)
            {
                ConvertLinksToHAL(apiHost, objToReturn, neoJsonObj);
            }

            return objToReturn;
        }

        /// <summary>
        /// Converts the Links collection into a HAL format
        /// </summary>
        /// <param name="apiHost"></param>
        /// <param name="objToReturn"></param>
        /// <param name="neoJsonObj"></param>
        private static void ConvertLinksToHAL(string apiHost, JObject objToReturn, JObject neoJsonObj)
        {
            JArray existingLinksAsJsonObj = (JArray)neoJsonObj["_links"];

            var linksJObject = new JObject();

            if (!existingLinksAsJsonObj.All(x => string.IsNullOrWhiteSpace(x["href"].ToString())))
            {
                linksJObject.Add("self", neoJsonObj["data"]["uri"]);

                var curiesJArray = new JArray();

                var curiesJObject = new JObject();
                curiesJObject.Add("name", "cont");
                curiesJObject.Add("href", apiHost);

                curiesJArray.Add(curiesJObject);

                linksJObject.Add(new JProperty("curies", curiesJArray));

                Dictionary<string, List<JObject>> relationshipGroupings = new Dictionary<string, List<JObject>>();

                foreach (var child in existingLinksAsJsonObj)
                {
                    var childKey = child["relationship"].ToString();

                    var uri = new Uri(child["href"].ToString());

                    var jObjectToAdd = new JObject(new JProperty("href", $"/{child["contentType"]}/{uri.Segments.LastOrDefault().Trim('/')}".ToLowerInvariant()), new JProperty("title", child["title"]), new JProperty("contentType", child["contentType"]));

                    foreach (var prop in child["props"])
                    {
                        jObjectToAdd.Add(prop);
                    }

                    if (relationshipGroupings.ContainsKey(childKey))
                    {
                        relationshipGroupings[childKey].Add(jObjectToAdd);
                    }
                    else
                    {
                        relationshipGroupings.Add(childKey, new List<JObject>() { jObjectToAdd });
                    }
                }

                foreach (var group in relationshipGroupings)
                {
                    if (group.Value.Count > 1)
                    {
                        linksJObject.Add(new JProperty($"cont:{group.Key}", new JArray { group.Value }));
                    }
                    else
                    {
                        linksJObject.Add(new JProperty($"cont:{group.Key}", group.Value.FirstOrDefault()));
                    }
                }
            }

            objToReturn.Add(new JProperty("_links", linksJObject));
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
