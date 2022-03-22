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

namespace DFC.Api.Content.Function
{
    public class Expand : BaseFunction
    {
        public Expand(IDataSourceProvider dataSource, IJsonFormatHelper jsonFormatHelper)
        {
            _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
            _jsonFormatHelper = jsonFormatHelper ?? throw new ArgumentNullException(nameof(jsonFormatHelper));
        }

        [FunctionName("Expand")]
        public async Task<IActionResult> Run(
            [HttpTrigger(
                AuthorizationLevel.Anonymous,
                "get",
                Route = "Expand/{contentType}/{id:guid}/{publishState?}")]
            HttpRequest request,
            string contentType,
            Guid id,
            ILogger log,
            string publishState = "")
        {
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
                record = _jsonFormatHelper.BuildSingleResponse(record, true);
                
                const int level = 0;
                await GetChildItems(
                    record,
                    publishState,
                    log, 
                    new Dictionary<int, List<string>> { { level, new List<string> { $"{contentType}{id}" } } },
                    level,
                    true);
                
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

        private async Task GetChildItems(
            Dictionary<string, object> record,
            string publishState,
            ILogger log,
            Dictionary<int, List<string>> retrievedCompositeKeys,
            int level,
            bool multiDirectional)
        {
            if (!record.ContainsKey("_links"))
            {
                return;
            }
            
            var recordLinks = _jsonFormatHelper.SafeCastToDictionary(record["_links"]);

            var children = new List<Dictionary<string, object>>();
            var childIds = recordLinks!
                .Where(previousItemLink =>
                    previousItemLink.Key != "self" && previousItemLink.Key != "curies")
                .Select(previousItemLink => _jsonFormatHelper.SafeCastToDictionary(previousItemLink.Value))
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

                for (var index = 0; index < childResults.Count; index++)
                {
                    childResults[index] = _jsonFormatHelper.BuildSingleResponse(childResults[index], multiDirectional);

                    await GetChildItems(
                        childResults[index],
                        publishState,
                        log,
                        retrievedCompositeKeys,
                        level + 1,
                        multiDirectional);
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