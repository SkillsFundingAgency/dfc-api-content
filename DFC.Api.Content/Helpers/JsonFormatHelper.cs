﻿using DFC.Api.Content.Enums;
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
        private readonly IOptionsMonitor<ContentTypeMapSettings> _settings;

        public JsonFormatHelper(IOptionsMonitor<ContentTypeMapSettings> settings)
        {
            _settings = settings;
        }

        public string FormatResponse(IEnumerable<IRecord> recordsResult, RequestType type)
        {
            switch (type)
            {
                case RequestType.GetAll:
                    return this.ReplaceNamespaces(recordsResult.SelectMany(z => z.Values).Select(y => y.Value));
                case RequestType.GetById:
                    return this.CreateSingleRootObject(this.ReplaceNamespaces(recordsResult.Select(z => z.Values).FirstOrDefault().Values.FirstOrDefault()));
                default:
                    throw new NotSupportedException($"Request Type: {type} not supported");
            }
        }

        private string CreateSingleRootObject(object input)
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

        private string ReplaceNamespaces(object input)
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