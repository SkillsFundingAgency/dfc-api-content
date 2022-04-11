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
                    record,
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
            Dictionary<string, object> record,
            string publishState,
            ILogger log,
            Dictionary<int, List<string>> retrievedCompositeKeys,
            int level,
            bool multiDirectional,
            int maxDepth,
            List<string> typesToInclude)
        {
            if (!record.ContainsKey("_links"))
            {
                return;
            }
            
            var children = new List<Dictionary<string, object>>();
            
            if (level > maxDepth)
            {
                record["ContentItems"] = children;
                return;
            }

            var recordLinks = _jsonFormatHelper.SafeCastToDictionary(record["_links"]);
            var childIds = GetChildIds(recordLinks);
            
            foreach (var childIdGroup in childIds)
            {
                var queryParameters = new QueryParameters(
                    childIdGroup.Key.ToLower(),
                    childIdGroup
                        .Where(grp => !AncestorsContainsCompositeKey(grp.ContentType, grp.Id, retrievedCompositeKeys, level))
                        .Where(grp => typesToInclude.Contains(grp.ContentType))
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
                    retrievedCompositeKeys[level].Add($"{childIdGroup.Key}{id}");
                }

                for (var index = 0; index < childResults.Count; index++)
                {
                    childResults[index] = _jsonFormatHelper.BuildSingleResponse(childResults[index], multiDirectional);

                    await GetChildItems(
                        childResults[index],
                        publishState,
                        log,
                        retrievedCompositeKeys,
                        level + 1,
                        multiDirectional,
                        maxDepth,
                        typesToInclude);
                }

                children.AddRange(childResults);
            }

            record["ContentItems"] = children;
        }

        private List<IGrouping<string, (string ContentType, Guid Id)>> GetChildIds(Dictionary<string, object> recordLinks)
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

            return childIdsObjects
                .Union(childIdsArrays)
                .Select(dict => (string) dict!["href"])
                .Where(uri => !string.IsNullOrEmpty(uri))
                .Select(GetContentTypeAndId)
                .GroupBy(contentTypeAndId => contentTypeAndId.ContentType)
                .ToList();
        }

        private static bool AncestorsContainsCompositeKey(
            string contentType,
            Guid? id,
            Dictionary<int, List<string>> retrievedCompositeKeys,
            int level)
        {
            var compositeKey = contentType + id;
            
            for (var currentLevel = level; currentLevel >= 0; currentLevel--)
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