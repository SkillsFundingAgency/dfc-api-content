﻿using DFC.Api.Content.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using DFC.Api.Content.Interfaces;
using Newtonsoft.Json.Linq;

namespace DFC.Api.Content.Helpers
{
    public class JsonFormatHelper : IJsonFormatHelper
    {
        private static readonly List<string> CosmosBuiltInPropsToIgnore = new List<string>
        {
            "_rid",
            "_self",
            "_etag",
            "_attachments",
            "_ts",
        };

        public object FormatResponse(
            List<Dictionary<string, object>> records,
            RequestType type,
            bool multiDirectional)
        {
            return type switch
            {
                RequestType.GetAll => SummaryFormat(records),
                RequestType.GetById => BuildSingleResponse(records.Single(), multiDirectional, string.Empty),
                _ => throw new NotSupportedException($"Request Type: {type} not supported")
            };
        }
        
        public Dictionary<string, object> ExpandIncomingLinksToContItems(Dictionary<string, object> record, bool multiDirectional, string incomingMarkerKey)
        {
            // Expand the incoming links into their own section
            var recordLinks = SafeCastToDictionary(record["_links"]);

            if (recordLinks == null)
            {
                throw new MissingFieldException("Links property missing");
            }
            
            var curies = (recordLinks["curies"] as JArray)!.ToObject<List<Dictionary<string, object>>>();

            var incomingPosition = curies!.FindIndex(curie =>
                (string)curie["name"] == "incoming");
                
            var incomingObject = curies.Count > incomingPosition ? curies[incomingPosition] : null;

            if (incomingObject == null)
            {
                throw new MissingFieldException("Incoming property missing");
            }
                
            var incomingList = (incomingObject["items"] as JArray)!.ToObject<List<Dictionary<string, object>>>();
                
            foreach (var incomingItem in incomingList!)
            {
                var contentType = (string)incomingItem["contentType"];
                var id = (string)incomingItem["id"];
                var key = $"cont:has{FirstCharToUpper(contentType)}";
                
                var value = new Dictionary<string, object>
                {
                    {"href", multiDirectional ? $"/{contentType}/{id}/true" : $"/{contentType}/{id}"},
                    {"contentType", contentType}
                };

                if (incomingItem.ContainsKey("twoWay") && (bool)incomingItem["twoWay"])
                {
                    value.Add("twoWay", true);
                }
                
                if (!string.IsNullOrEmpty(incomingMarkerKey))
                {
                    value.Add(incomingMarkerKey, true);
                }

                if (!recordLinks.ContainsKey(key))
                {
                    recordLinks.Add(key, value);
                    continue;
                }
                
                if (recordLinks[key] is List<Dictionary<string, object>> list)
                {
                    list.Add(value);
                    continue;
                }
                
                recordLinks[key] = new List<Dictionary<string, object>>
                {
                    (Dictionary<string, object>)recordLinks[key],
                    value
                };
            }
                
            record["_links"] = recordLinks!;
            return record;
        }
        
        public Dictionary<string, object> SafeCastToDictionary(object value)
        {
            if (value is JObject valObj)
            {
                return valObj.ToObject<Dictionary<string, object>>() ?? throw new Exception("JObject could not be cast to dictionary");
            }

            if (!(value is Dictionary<string, object> dictionary))
            {
                throw new ArgumentException($"Didn't expect type {value.GetType().Name}");
            }
            
            return dictionary;
        }
        
        public List<Dictionary<string, object>> SafeCastToList(object value)
        {
            if (value is JArray valAry)
            {
                return valAry.ToObject<List<Dictionary<string, object>>>() ?? throw new Exception("JArray could not be cast to list");
            }
            
            return (List<Dictionary<string, object>>)value;
        }

        private static List<Dictionary<string, object?>> SummaryFormat(List<Dictionary<string, object>> records)
        {
            var returnList = new List<Dictionary<string, object?>>();

            foreach (var record in records)
            {
                returnList.Add(new Dictionary<string, object?>
                {
                    { "skos__prefLabel", record["skos__prefLabel"] },
                    { "CreatedDate", record.ContainsKey("CreatedDate") ? record["CreatedDate"] : null },
                    { "ModifiedDate", record["ModifiedDate"] },
                    { "Uri", record["uri"] }
                });
            }
            
            return returnList;
        }

        public Dictionary<string, object> BuildSingleResponse(Dictionary<string, object> record, bool multiDirectional, string incomingMarkerKey)
        {
            if (multiDirectional)
            {
                record = ExpandIncomingLinksToContItems(record, true, incomingMarkerKey);
                record = AddMultiDirectionalProperty(record);
            }
            
            return StripUndesiredProperties(record);
        }

        public Dictionary<string, object> AddMultiDirectionalProperty(Dictionary<string, object> record)
        {
            var recordLinks = SafeCastToDictionary(record["_links"]);
            var newRecordLinks = new Dictionary<string, object>();
            
            foreach (var recordLink in recordLinks)
            {
                if (recordLink.Key == "self" || recordLink.Key == "curies")
                {
                    newRecordLinks.Add(recordLink.Key, recordLink.Value);
                    continue;
                }

                if (recordLink.Value is JObject || recordLink.Value is Dictionary<string, object>)
                {
                    var dict = SafeCastToDictionary(recordLink.Value);
                    var href = (string?) dict["href"];

                    if (href?.EndsWith("/true") != false)
                    {
                        newRecordLinks.Add(recordLink.Key, recordLink.Value);
                        continue;
                    }

                    if (!string.IsNullOrEmpty(href))
                    {
                        dict["href"] = $"{href}/true";
                    }
                    
                    newRecordLinks.Add(recordLink.Key, dict);
                }
                else
                {
                    var list = SafeCastToList(recordLink.Value);

                    foreach (var dict in list)
                    {
                        var href = (string?)dict["href"];

                        if (href?.EndsWith("/true") != false)
                        {
                            continue;
                        }
                        
                        if (!string.IsNullOrEmpty(href))
                        {
                            dict["href"] = $"{href}/true";
                        }
                    }
                    
                    newRecordLinks.Add(recordLink.Key, list);
                }
            }
            
            record["_links"] = newRecordLinks;
            return record;
        }

        private static Dictionary<string, object> StripUndesiredProperties(Dictionary<string, object> record)
        {
            var returnProperties = new Dictionary<string, object>();

            foreach (var propertyName in record.Keys)
            {
                if (CosmosBuiltInPropsToIgnore.Contains(propertyName))
                {
                    continue;
                }

                var propertyValue = record[propertyName];
                returnProperties.Add(propertyName, propertyValue);
            }
            
            return returnProperties;
        }
        
        private static string FirstCharToUpper(string input) =>
            input switch
            {
                null => throw new ArgumentNullException(nameof(input)),
                "" => throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input)),
                _ => string.Concat(input[0].ToString().ToUpper(), input.AsSpan(1))
            };
    }
}
