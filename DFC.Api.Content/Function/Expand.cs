using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using DFC.Api.Content.Enums;
using DFC.Api.Content.Exceptions;
using DFC.Api.Content.Interfaces;
using DFC.Api.Content.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace DFC.Api.Content.Function
{
    public class Expand
    {
        private readonly IDataSourceProvider _dataSource;
        
        public Expand(IDataSourceProvider dataSource)
        {
            _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
        }

        [FunctionName("Expand")]
        public async Task<IActionResult> Run(
            [HttpTrigger(
                AuthorizationLevel.Anonymous,
                "get",
                Route = "Expand/{contentType}/{id:guid}/{publishState?}/{multiDirectional:bool?}")]
            HttpRequest request,
            string contentType,
            Guid id,
            ILogger log,
            bool? multiDirectional,
            string publishState = "")
        {
            MaintainBackwardsCompatibilityWithPath(ref publishState, ref multiDirectional);
            SetDefaultStateIfEmpty(ref publishState, request);

            try
            {
                var environment = Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT");
                log.LogInformation("Function has been triggered in {Environment} environment", environment);

                if (string.IsNullOrWhiteSpace(contentType))
                {
                    throw ApiFunctionException.BadRequest("Required parameter contentType not found in path.");
                }
                
                var queryParameters = new QueryParameters(contentType.ToLower(), new List<Guid?> { id });
                var queryToExecute = BuildQuery(queryParameters, publishState);
                var recordsResult = await ExecuteQuery(queryToExecute, log);

                if (!recordsResult.Any())
                {
                    return new NotFoundObjectResult("Resource not found");
                }
                
                const int level = 0;
                await GetChildItems(
                    recordsResult.First(),
                    publishState,
                    log, 
                    new Dictionary<int, List<string>> { { level, new List<string> { contentType + id } } },
                    level,
                    multiDirectional ?? false);
                
                log.LogInformation("Request has successfully been completed with results");
                SetContentTypeHeader(request);

                return new OkObjectResult(recordsResult);
            }
            catch (ApiFunctionException e)
            {
                // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
                log.LogError(e.ToString());
                return e.ActionResult ?? new InternalServerErrorResult();
            }
            catch (Exception e)
            {
                // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
                log.LogError(e.ToString());
                return new InternalServerErrorResult();
            }
        }

        private async Task GetChildItems(
            Dictionary<string, object> record,
            string publishState,
            ILogger log,
            Dictionary<int, List<string>> retrievedCompositeKeys,
            int level,
            bool multiDirectional)
        {
            var recordLinks = (record["_links"] as JObject)!.ToObject<Dictionary<string, object>>();

            if (multiDirectional)
            {
                record = ExpandIncomingLinksToContItems(record);
            }
            
            var children = new List<Dictionary<string, object>>();
            var childIds = recordLinks!
                .Where(previousItemLink =>
                    previousItemLink.Key != "self" && previousItemLink.Key != "curies")
                .Select(previousItemLink =>
                    ((JObject) previousItemLink.Value).ToObject<Dictionary<string, object>>())
                .Select(dict => (string) dict!["href"])
                .Where(uri => !string.IsNullOrEmpty(uri))
                .Select(GetContentTypeAndId)
                .GroupBy(contentTypeAndId => contentTypeAndId.ContentType)
                .ToList();

            foreach (var childIdGroup in childIds)
            {
                var queryParameters = new QueryParameters(
                    childIdGroup.Key.ToLower(),
                    childIdGroup
                        .Where(grp => !AncestorsContainsCompositeKey(grp.ContentType, grp.Id, retrievedCompositeKeys, level))
                        .Select(grp => (Guid?)grp.Id).ToList());

                if (!queryParameters.Ids.Any())
                {
                    continue;
                }
                
                var queryToExecute = BuildQuery(queryParameters, publishState);
                var childResults = await ExecuteQuery(queryToExecute, log);

                if (!retrievedCompositeKeys.ContainsKey(level))
                {
                    retrievedCompositeKeys.Add(level, new List<string>());
                }
                
                foreach (var id in queryParameters.Ids)
                {
                    retrievedCompositeKeys[level].Add(childIdGroup.Key + id);
                }
                
                foreach (var childResult in childResults)
                {
                    await GetChildItems(childResult, publishState, log, retrievedCompositeKeys, level + 1, multiDirectional);
                }
                
                children.AddRange(childResults);
            }

            record["ContentItems"] = children;
        }
        
        private static bool AncestorsContainsCompositeKey(
            string contentType,
            Guid? id,
            Dictionary<int, List<string>> retrievedCompositeKeys,
            int level)
        {
            var compositeKey = contentType + id;
            
            for (var currentLevel = level - 1; currentLevel >= 0; currentLevel--)
            {
                if (!retrievedCompositeKeys.ContainsKey(currentLevel))
                {
                    continue;
                }

                var levelsList = retrievedCompositeKeys[currentLevel];

                if (levelsList.Contains(compositeKey))
                {
                    return true;
                }
            }

            return false;
        }
        
        private static (string ContentType, Guid Id) GetContentTypeAndId(string uri)
        {
            var id = Guid.Parse(uri.Split('/')[uri.Split('/').Length - 1]);
            var contentType = uri.Split('/')[uri.Split('/').Length - 2].ToLower();

            return (contentType, id);
        }
        
        private static void MaintainBackwardsCompatibilityWithPath(ref string publishState, ref bool? multiDirectional)
        {
            if (!bool.TryParse(publishState, out var multiDirectionalOutput)) return;

            multiDirectional = multiDirectionalOutput;
            publishState = string.Empty;
        }
        
        private static void SetDefaultStateIfEmpty(ref string publishState, HttpRequest request)
        {
            if (!string.IsNullOrEmpty(publishState)) return;
            
            publishState = GetDefaultPublishState(request.Host.Host);
        }
        
        private static string GetDefaultPublishState(string host)
        {
            const string previewPublishState = "preview";
            const string previewPublishStateAlt = "draft";
            const string publishedPublishState = "publish";
            
            if (host.Contains(previewPublishState, StringComparison.InvariantCultureIgnoreCase) ||
                host.Contains(previewPublishStateAlt, StringComparison.InvariantCultureIgnoreCase))
            {
                return previewPublishState;
            }
            
            return publishedPublishState;
        }
        
        private async Task<List<Dictionary<string, object>>> ExecuteQuery(ExecuteQuery query, ILogger log)
        {
            log.LogInformation("Attempting to query data source with the following query: {QueryText}, {ContentType}",
                query.QueryText,
                query.ContentType);

            try
            {
                return (await _dataSource.Run(new GenericQuery(query.QueryText, query.ContentType, query.PublishState))).ToList();
            }
            catch (Exception ex)
            {
                throw ApiFunctionException.InternalServerError("Unable to run query", ex);
            }
        }
        
        private static ExecuteQuery BuildQuery(QueryParameters queryParameters, string publishState)
        {
            const string contentByIdCosmosSql = "select * from c where c.id in ({0})";
            
            return new ExecuteQuery(
                string.Format(contentByIdCosmosSql, string.Join(',', queryParameters.Ids.Select(id => $"'{id}'").ToArray())),
                RequestType.GetById,
                queryParameters.ContentType,
                publishState);
        }
        
        private static void SetContentTypeHeader(HttpRequest req)
        {
            var headers = req.HttpContext.Response.Headers;
            
            headers.Remove("content-type");
            headers.Add("content-type", "application/hal+json");
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