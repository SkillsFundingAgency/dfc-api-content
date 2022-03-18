using DFC.Api.Content.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace DFC.Api.Content.Helpers
{
    public class JsonFormatHelper : IJsonFormatHelper
    {
        private static readonly List<string> CosmosBuiltInProps = new List<string>
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
            string apiHost,
            bool multiDirectional)
        {
            return type switch
            {
                RequestType.GetAll => SummaryFormat(records),
                RequestType.GetById => BuildSingleResponse(records.Single(), multiDirectional),
                _ => throw new NotSupportedException($"Request Type: {type} not supported")
            };
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

        private static Dictionary<string, object> BuildSingleResponse(Dictionary<string, object> record, bool multiDirectional)
        {
            if (multiDirectional)
            {
                record = ExpandIncomingLinksToContItems(record);
            }
            
            return StripUndesiredProperties(record);
        }
        
        private static Dictionary<string, object> StripUndesiredProperties(Dictionary<string, object> record)
        {
            var returnProperties = new Dictionary<string, object>();

            foreach (var propertyName in record.Keys)
            {
                if (CosmosBuiltInProps.Contains(propertyName))
                {
                    continue;
                }

                var propertyValue = record[propertyName];
                returnProperties.Add(propertyName, propertyValue);
            }
            
            return returnProperties;
        }

        private static Dictionary<string, object> ExpandIncomingLinksToContItems(Dictionary<string, object> record)
        {
            // Expand the incoming links into their own section
            var recordLinks = (record["_links"] as JObject)!.ToObject<Dictionary<string, object>>();
            var curies = (recordLinks?["curies"] as JArray)!.ToObject<List<Dictionary<string, object>>>();

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

                recordLinks.Add($"cont:has{FirstCharToUpper(contentType)}", new Dictionary<string, object>
                {
                    { "href", $"/{contentType}/{id}" }
                });
            }
                
            record["_links"] = recordLinks;
            return record;
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
