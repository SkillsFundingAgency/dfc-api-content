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
using MoreLinq.Extensions;
using Newtonsoft.Json.Linq;

namespace DFC.Api.Content.Function
{
    public class Expand : BaseFunction
    {
        private const string ContentItemsKey = "ContentItems";
        
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

        private async Task<ExpandPostData?> GetPostParameters(HttpRequest request)
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

        private Dictionary<string, Dictionary<Guid, List<int>>> GetChildIdsByType(
            List<Dictionary<string, object>> records,
            Dictionary<int, List<string>> retrievedCompositeKeys,
            int level,
            List<string> typesToInclude)
        {
            var childIdsByType = new Dictionary<string, Dictionary<Guid, List<int>>>();
            
            records.ForEach((record, recordIndex) =>
            {
                if (!record.ContainsKey("_links"))
                {
                    return;
                }
                
                var recordLinks = _jsonFormatHelper.SafeCastToDictionary(record["_links"]);
                PopulateChildIdsByType(recordLinks, recordIndex, retrievedCompositeKeys, level, typesToInclude, ref childIdsByType);
            });

            return childIdsByType;
        }

        private Dictionary<int, List<Dictionary<string, object>>> GetChildIdsByRecordPosition(
            List<Dictionary<string, object>> records,
            List<Dictionary<string, object>> allChildren,
            Dictionary<string, Dictionary<Guid, List<int>>> childIdsByType)
        {
            var childIdsByRecordPosition = records.Select((_, index) =>
                    new KeyValuePair<int, List<Dictionary<string, object>>>(index,
                        new List<Dictionary<string, object>>()))
                .ToDictionary();
            
            foreach (var childResult in allChildren)
            {
                var itemId = Guid.Parse((string)childResult["id"]);
                var recordPositions = childIdsByType
                    .Where(childId => childId.Value.Any(idPositions => idPositions.Key == itemId))
                    .SelectMany(contentTypeGroup => contentTypeGroup.Value)
                    .Where(group => group.Key == itemId)
                    .SelectMany(group => group.Value);

                foreach (var recordPosition in recordPositions)
                {
                    childIdsByRecordPosition[recordPosition].Add(childResult);
                }
            }

            return childIdsByRecordPosition;
        }

        private static void ResetContentItems(List<Dictionary<string, object>> records)
        {
            records.ForEach(record => record[ContentItemsKey] = new List<Dictionary<string, object>>());
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
            ResetContentItems(records);
            
            if (level > maxDepth)
            {
                return;
            }
            
            // Make sure we can record that we fetched these records
            if (!retrievedCompositeKeys.ContainsKey(level))
            {
                retrievedCompositeKeys.Add(level, new List<string>());
            }

            var retrievedCompositeKeyLevel = retrievedCompositeKeys[level];

            var allChildren = new List<Dictionary<string, object>>();
            var childIdsByType = GetChildIdsByType(records, retrievedCompositeKeys, level, typesToInclude);
            var childRelationships = GetChildRelationships(records);

            foreach (var (contentType, idGroup) in childIdsByType)
            {
                var queryParameters = new QueryParameters(
                    contentType.ToLower(),
                    idGroup.Select(grp => (Guid?) grp.Key).ToList());

                if (!queryParameters.Ids.Any())
                {
                    continue;
                }

                var queryToExecute = BuildQuery(queryParameters, publishState);
                var childResults = await ExecuteQuery(queryToExecute, log);

                // Record it was fetched
                queryParameters.Ids.ForEach(queryParameterId =>
                    retrievedCompositeKeyLevel.Add($"{queryParameters.ContentType}{queryParameterId}"));
                
                for (var index = 0; index < childResults.Count; index++)
                {
                    var childResult = childResults[index];
                    var (_, id, _) = GetContentTypeIdAndPosition((string) childResult["uri"], default);

                    var propertiesToAdd = childRelationships.Any(x => x.Id == id)
                        ? childRelationships.Single(x => x.Id == id).childRelationship
                        : new Dictionary<string, object>();

                    foreach (var (key, value) in propertiesToAdd.Where(x => x.Key != "href"))
                    {
                        childResult.Add(key, value);
                    }
                    
                    childResults[index] = _jsonFormatHelper.BuildSingleResponse(childResult, multiDirectional);
                }
                
                allChildren.AddRange(childResults);
            }

            foreach (var (recordPosition, children) in GetChildIdsByRecordPosition(records, allChildren, childIdsByType))
            {
                var contentItems = (List<Dictionary<string, object>>)records[recordPosition][ContentItemsKey];
                contentItems.AddRange(children);

                records[recordPosition][ContentItemsKey] = contentItems;
            }
            
            await GetChildItems(
                allChildren,
                publishState,
                log,
                retrievedCompositeKeys,
                level + 1,
                multiDirectional,
                maxDepth,
                typesToInclude);
        }

        private List<(Guid Id, Dictionary<string, object> childRelationship)> GetChildRelationships(
            List<Dictionary<string, object>> records)
        {
            var returnList = new List<Dictionary<string, object>>();

            foreach (var record in records)
            {
                var recordLinks = _jsonFormatHelper.SafeCastToDictionary(record["_links"]);
                var filteredRecordLinks = GetFilteredRecordLinks(recordLinks);

                var childIdsObjects = filteredRecordLinks
                    .Where(previousItemLink => previousItemLink.Value is JObject 
                        || previousItemLink.Value is Dictionary<string, object>)
                    .Select(previousItemLink => _jsonFormatHelper.SafeCastToDictionary(previousItemLink.Value));

                var childIdsArrays = filteredRecordLinks
                    .Where(previousItemLink => previousItemLink.Value is JArray
                        || previousItemLink.Value is List<Dictionary<string, object>>)
                    .Select(previousItemLink => _jsonFormatHelper.SafeCastToList(previousItemLink.Value))
                    .SelectMany(list => list);                
                
                returnList.AddRange(childIdsObjects.Union(childIdsArrays));
            }

            return returnList
                .Where(childRelationship => !string.IsNullOrEmpty((string) childRelationship["href"]))
                .Select(childRelationship =>
                    (GetContentTypeIdAndPosition((string) childRelationship["href"], default).Id, childRelationship))
                .GroupBy(idEtc => idEtc.Id)
                .Select(idEtcGroup => idEtcGroup.First())
                .ToList();
        }
        
        private List<KeyValuePair<string, object>> GetFilteredRecordLinks(Dictionary<string, object> recordLinks)
        {
            return recordLinks
                .Where(previousItemLink =>
                    previousItemLink.Key != "self" && previousItemLink.Key != "curies")
                .ToList();
        }
        
        private void PopulateChildIdsByType(
            Dictionary<string, object> recordLinks,
            int parentPosition,
            Dictionary<int, List<string>> retrievedCompositeKeys,
            int level,
            List<string> typesToInclude,
            ref Dictionary<string, Dictionary<Guid, List<int>>> childIdsByType)
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

                foreach (var contentTypeWithId in contentTypeGrouping)
                {
                    if (!childIdByType.ContainsKey(contentTypeWithId.Id))
                    {
                        childIdByType.Add(contentTypeWithId.Id, new List<int>());
                    }
                    
                    if (!AncestorsContainsCompositeKey(contentTypeWithId.ContentType.ToLower(), contentTypeWithId.Id, retrievedCompositeKeys, level)
                        && typesToInclude.Contains(contentTypeWithId.ContentType.ToLower()))
                    {
                        childIdByType[contentTypeWithId.Id].Add(contentTypeWithId.ParentPosition);
                    }
                }

                childIdsByType[contentTypeGrouping.Key] = childIdByType;
            }

            var contentTypeKeys = childIdsByType.Keys.ToList();
            
            // Remove unused
            for (int idx = 0, len = contentTypeKeys.Count; idx < len; idx++)
            {
                var contentTypeKey = contentTypeKeys.ElementAt(idx);
                var contentType = childIdsByType[contentTypeKey];
                var count = contentType.Sum(kvp => kvp.Value.Count);

                if (count == 0)
                {
                    childIdsByType.Remove(contentTypeKey);
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
        
        private static Queries BuildQuery(QueryParameters queryParameters, string publishState)
        {
            var queries = new List<Query>();
            
            if (queryParameters.Ids.Count == 1)
            {
                const string contentByIdSingleCosmosSql = "select * from c where c.id = @id0";

                queries.Add(
                    new Query(
                        contentByIdSingleCosmosSql,
                        new Dictionary<string, object>
                        {
                            { "@id0", queryParameters.Ids.Single()!.Value.ToString() }
                        }));
            }
            else
            {
                const string contentByIdMultipleCosmosSql = "select * from c where c.id in ({0})";
                var idGroups = queryParameters.Ids.Batch(500);

                foreach (var idGroup in idGroups)
                {
                    var parameters = new Dictionary<string, object>();
                    var index = 0;
                    
                    var sqlQuery = string.Format(contentByIdMultipleCosmosSql,
                        string.Join(',', idGroup.Select(id => $"@id{index++}").ToArray()));

                    index = 0;
                
                    foreach (var id in idGroup)
                    {
                        parameters.Add($"@id{index++}", id!.Value.ToString());                    
                    }
                    
                    queries.Add(new Query(sqlQuery, parameters));
                }
            }
            
            return new Queries(
                queries.ToArray(),
                RequestType.GetById,
                queryParameters.ContentType,
                publishState);
        }
    }
}