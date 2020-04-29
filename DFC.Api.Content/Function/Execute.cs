using DFC.ServiceTaxonomy.ApiFunction.Helpers;
using DFC.ServiceTaxonomy.ApiFunction.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using DFC.ServiceTaxonomy.ApiFunction.Exceptions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using Neo4j.Driver;
using System.Runtime.CompilerServices;

//todo: update to func v3, core 3.1, c# 8
//todo: nullable reference types
//todo: sonar

namespace DFC.ServiceTaxonomy.ApiFunction.Function
{
    public class Execute
    {
        private readonly IOptionsMonitor<ContentTypeMapSettings> _contentTypeMapSettings;
        private readonly INeo4JHelper _neo4JHelper;

        private static string contentByIdCypher = "MATCH (n {{uri:'{0}'}}) return n;";
        private static string contentGetAllCypher = "MATCH (n:{0}) return n;";

        public Execute(IOptionsMonitor<ServiceTaxonomyApiSettings> serviceTaxonomyApiSettings, IOptionsMonitor<ContentTypeMapSettings> contentTypeMapSettings, INeo4JHelper neo4JHelper)
        {
            _contentTypeMapSettings = contentTypeMapSettings ?? throw new ArgumentNullException(nameof(contentTypeMapSettings));
            _neo4JHelper = neo4JHelper ?? throw new ArgumentNullException(nameof(neo4JHelper));
        }

        [FunctionName("Execute")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Execute/{contentType}/{id:guid?}")] HttpRequest req, string contentType, Guid? id,
            ILogger log, ExecutionContext context)
        {
            try
            {
                var environment = Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT");
                log.LogInformation($"Function has been triggered in {environment} environment.");

                bool development = environment == "Development";

                if (string.IsNullOrWhiteSpace(contentType))
                {
                    throw ApiFunctionException.BadRequest($"Required parameter contentType not found in path.");
                }

                var queryParameters = new QueryParameters { ContentType = contentType.ToLower(), Id = id };

                //Could move in to helper class
                var queryToExecute = this.BuildQuery(queryParameters, req.Path.Value);

                object recordsResult = await ExecuteCypherQuery(queryToExecute, log);

                if (recordsResult == null)
                    return new NoContentResult();

                log.LogInformation("request has successfully been completed with results");

                return new OkObjectResult(recordsResult);
            }
            catch (ApiFunctionException e)
            {
                log.LogError(e.ToString());
                return e.ActionResult;
            }
            catch (Exception e)
            {
                log.LogError(e.ToString());
                return new InternalServerErrorResult();
            }
        }

        private async Task<object> ExecuteCypherQuery(string query, ILogger log)
        {
            log.LogInformation($"Attempting to query neo4j with the following query: {query}");

            try
            {   
                return await _neo4JHelper.ExecuteCypherQueryInNeo4JAsync(query, new Dictionary<string, object>());
            }
            catch (Exception ex)
            {
                throw ApiFunctionException.InternalServerError("Unable To run query", ex);
            }
        }

        private string BuildQuery(QueryParameters queryParameters, string requestPath)
        {
            if (!queryParameters.Id.HasValue)
            {
                //GetAll Query
                return string.Format(contentGetAllCypher, MapContentTypeToNamespace(queryParameters.ContentType));
            }
            else
            {

                if (_contentTypeMapSettings.CurrentValue.OverrideUri)
                {
                    //Change to just use URI when neo has been updated
                    var uri = $"http://nationalcareers.service.gov.uk/{queryParameters.ContentType}/{queryParameters.Id}";
                    return string.Format(contentByIdCypher, uri);
                }
                else
                {
                    return string.Format(contentByIdCypher, requestPath);
                }
            }
        }

        private string MapContentTypeToNamespace(string contentType)
        {
            _contentTypeMapSettings.CurrentValue.Values.TryGetValue(contentType.ToLower(), out string mappedValue);

            if (string.IsNullOrWhiteSpace(mappedValue))
            {
                throw ApiFunctionException.BadRequest($"Content Type {contentType} is not mapped in AppSettings");
            }

            return mappedValue;
        }
    }
}
