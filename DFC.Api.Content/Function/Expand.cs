using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
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
    public class Expand : BaseFunction
    {
        public Expand(IDataSourceProvider dataSource, IJsonFormatHelper jsonFormatHelper)
        {
            _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
            _jsonFormatHelper = jsonFormatHelper ?? throw new ArgumentNullException(nameof(jsonFormatHelper));
        }

        [HttpPost]
        [FunctionName("Expand")]
        public async Task<IActionResult> Run(
            [HttpTrigger(
                AuthorizationLevel.Anonymous,
                "post",
                Route = "Expand/{contentType}/{id:guid}/{publishState?}")]
            HttpRequest request,
            string contentType,
            Guid id,
            ILogger log,
            string publishState = "")
        {
            var parameters = await GetPostParameters(request);

            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            
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

                var record = recordsResult[0];
                record = _jsonFormatHelper.BuildSingleResponse(record, parameters.MultiDirectional);

                const int level = 0;
                await GetChildItems(
                    new List<Dictionary<string, object>> { record },
                    publishState,
                    log, 
                    new Dictionary<int, List<string>> { { level, new List<string> { $"{contentType}{id}" } } },
                    level + 1,
                    parameters.MultiDirectional,
                    parameters.MaxDepth,
                    parameters.TypesToInclude.ToList());
                
                log.LogInformation("Request has successfully been completed with results");
                SetContentTypeHeader(request);

                return new OkObjectResult(record);
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
        
        public async Task<ExpandPostData?> GetPostParameters(HttpRequest request)
        {
            if (!request.Body.CanSeek)
            {
                // We only do this if the stream isn't *already* seekable,
                // as EnableBuffering will create a new stream instance
                // each time it's called
                request.EnableBuffering();
            }

            request.Body.Position = 0;

            var reader = new StreamReader(request.Body, Encoding.UTF8);

            var body = await reader.ReadToEndAsync().ConfigureAwait(false);
            request.Body.Position = 0;

            return string.IsNullOrEmpty(body) ? null : JsonSerializer.Deserialize<ExpandPostData>(body);
        }
        
        private async Task GetChildItems(
            List<Dictionary<string, object>> records,
            string publishState,
            ILogger log,
            Dictionary<int, List<string>> retrievedCompositeKeys,
            int level,
            bool multiDirectional,
            int maxDepth,
            List<string> typesToInclude)
        {
            var childIdsByType = new Dictionary<string, Dictionary<Guid, List<int>>>();
            var childIdsByRecordPosition = new Dictionary<int, List<Dictionary<string, object>>>();
            
            for (var recordIndex = 0; recordIndex < records.Count; recordIndex++)
            {
                var record = records[recordIndex];
                if (!record.ContainsKey("_links"))
                {
                    continue;
                }
                
                var recordLinks = _jsonFormatHelper.SafeCastToDictionary(record["_links"]);
                PopulateChildIdsByType(recordLinks, childIdsByType, recordIndex, retrievedCompositeKeys, level, typesToInclude);
                
                childIdsByRecordPosition.Add(recordIndex, new List<Dictionary<string, object>>());
                record["ContentItems"] = new List<Dictionary<string, object>>();
            }

            if (level > maxDepth)
            {
                return;
            }
            
            var allChildResults = new List<Dictionary<string, object>>();
            
            foreach (var childIdGroup in childIdsByType)
            {
                var queryParameters = new QueryParameters(
                    childIdGroup.Key.ToLower(),
                    childIdGroup.Value.Select(grp => (Guid?) grp.Key).ToList());

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

                foreach (var queryParameterId in queryParameters.Ids)
                {
                    retrievedCompositeKeys[level].Add($"{childIdGroup.Key}{queryParameterId}");
                }

                for (var index = 0; index < childResults.Count; index++)
                {
                    childResults[index] = _jsonFormatHelper.BuildSingleResponse(childResults[index], multiDirectional);
                }
                
                allChildResults.AddRange(childResults);
            }
            
            foreach (var childResult in allChildResults)
            {
                var id = Guid.Parse((string)childResult["id"]);
                var positions = childIdsByType
                    .Where(childId => childId.Value.Any(y => y.Key == id))
                    .SelectMany(x => x.Value)
                    .Where(y => y.Key == id)
                    .SelectMany(x => x.Value);

                foreach (var pos in positions)
                {
                    childIdsByRecordPosition[pos].Add(childResult);
                }
            }
            
            foreach (var output in childIdsByRecordPosition)
            {
                var contentItems = (List<Dictionary<string, object>>)records[output.Key]["ContentItems"];
                contentItems.AddRange(output.Value);

                records[output.Key]["ContentItems"] = contentItems;
            }
            
            await GetChildItems(
                allChildResults,
                publishState,
                log,
                retrievedCompositeKeys,
                level + 1,
                multiDirectional,
                maxDepth,
                typesToInclude);
        }

        private void PopulateChildIdsByType(
            Dictionary<string, object> recordLinks,
            Dictionary<string, Dictionary<Guid, List<int>>> childIdsByType,
            int parentPosition,
            Dictionary<int, List<string>> retrievedCompositeKeys,
            int level,
            List<string> typesToInclude)
        {
            var filteredRecordLinks = recordLinks
                .Where(previousItemLink =>
                    previousItemLink.Key != "self" && previousItemLink.Key != "curies")
                .ToList();
            
            var childIdsObjects = filteredRecordLinks
                .Where(previousItemLink => previousItemLink.Value is JObject 
                    || previousItemLink.Value is Dictionary<string, object>)
                .Select(previousItemLink => _jsonFormatHelper.SafeCastToDictionary(previousItemLink.Value));

            var childIdsArrays = filteredRecordLinks
                .Where(previousItemLink => previousItemLink.Value is JArray ||
                    previousItemLink.Value is List<Dictionary<string, object>>)
                .Select(previousItemLink => _jsonFormatHelper.SafeCastToList(previousItemLink.Value))
                .SelectMany(list => list);

            var contentTypeGroupings = childIdsObjects
                .Union(childIdsArrays)
                .Select(dict => (string) dict!["href"])
                .Where(uri => !string.IsNullOrEmpty(uri))
                .Select(item => GetContentTypeIdAndPosition(item, parentPosition))
                .GroupBy(contentTypeAndId => contentTypeAndId.ContentType)
                .ToList();

            foreach (var contentTypeGrouping in contentTypeGroupings)
            {
                if (!childIdsByType.ContainsKey(contentTypeGrouping.Key))
                {
                    childIdsByType.Add(contentTypeGrouping.Key, new Dictionary<Guid, List<int>>());
                }
                
                var childIdByType = childIdsByType[contentTypeGrouping.Key];

                foreach (var x in contentTypeGrouping)
                {
                    if (!childIdByType.ContainsKey(x.Id))
                    {
                        childIdByType.Add(x.Id, new List<int>());
                    }
                    
                    if (!AncestorsContainsCompositeKey(x.ContentType.ToLower(), x.Id, retrievedCompositeKeys, level)
                        && typesToInclude.Contains(x.ContentType.ToLower()))
                    {
                        childIdByType[x.Id].Add(x.ParentPosition);
                    }
                }

                childIdsByType[contentTypeGrouping.Key] = childIdByType;
            }

            var keys = childIdsByType.Keys.ToList();
            
            // Remove unused
            for (int idx = 0, len = keys.Count; idx < len; idx++)
            {
                var ctKey = keys.ElementAt(idx);
                var ct = childIdsByType[ctKey];
                var c = 0;

                foreach (var kvp in ct)
                {
                    c += kvp.Value.Count;
                }

                if (c == 0)
                {
                    childIdsByType.Remove(ctKey);
                }
            }
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
        
        private static ExecuteQuery BuildQuery(QueryParameters queryParameters, string publishState)
        {
            const string contentByIdMultipleCosmosSql = "select * from c where c.id in ({0})";
            var sqlQuery = string.Format(contentByIdMultipleCosmosSql, string.Join(',', queryParameters.Ids.Select(id => $"'{id}'").ToArray()));
            
            if (queryParameters.Ids.Count == 1)
            {
                const string contentByIdSingleCosmosSql = "select * from c where c.id = '{0}'";
                sqlQuery = string.Format(contentByIdSingleCosmosSql, queryParameters.Ids.Single());
            }
            
            return new ExecuteQuery(
                sqlQuery,
                RequestType.GetById,
                queryParameters.ContentType,
                publishState);
        }
    }
}