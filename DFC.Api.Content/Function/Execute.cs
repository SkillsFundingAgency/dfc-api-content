using DFC.ServiceTaxonomy.ApiFunction.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Http;
using DFC.ServiceTaxonomy.ApiFunction.Exceptions;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using DFC.ServiceTaxonomy.Neo4j.Services;
using DFC.Api.Content.Models.Cypher;
using Neo4j.Driver;
using System.Linq;
using Newtonsoft.Json;
using DFC.Api.Content.Helpers;
using DFC.Api.Content.Enums;
using DFC.Api.Content.Models;

namespace DFC.ServiceTaxonomy.ApiFunction.Function
{
    public class Execute
    {
        private readonly IOptionsMonitor<ContentTypeMapSettings> _contentTypeMapSettings;
        private readonly IGraphDatabase _graphDatabase;
        private readonly IJsonFormatHelper _jsonFormatHelper;
        private const string contentByIdCypher = "MATCH (s {{uri:'{0}'}}) optional match(s)-[r]->(d) with s, {{href:d.uri, type:'GET', title:d.skos__prefLabel, dynamicKey:reduce(lab = '', n IN labels(d) | case n WHEN 'Resource' THEN '' ELSE n END), rel:labels(d)}} as destinationUris with s, apoc.map.fromValues([destinationUris.dynamicKey, {{href: destinationUris.href }}]) as map with s, collect(map) as links with s,links,{{ data: properties(s)}} as sourceNodeWithOutgoingRelationships return {{data:sourceNodeWithOutgoingRelationships.data, _links:links}}";

        private const string contentGetAllCypher = "MATCH (n:{0}) with {{properties: properties(n)}} as data return data.properties;";

        public Execute(IOptionsMonitor<ContentTypeMapSettings> contentTypeMapSettings, IGraphDatabase graphDatabase, IJsonFormatHelper jsonFormatHelper)
        {
            _contentTypeMapSettings = contentTypeMapSettings ?? throw new ArgumentNullException(nameof(contentTypeMapSettings));
            _graphDatabase = graphDatabase ?? throw new ArgumentNullException(nameof(graphDatabase));
            _jsonFormatHelper = jsonFormatHelper ?? throw new ArgumentNullException(nameof(jsonFormatHelper));
        }

        [FunctionName("Execute")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Execute/{contentType}/{id:guid?}")] HttpRequest req, string contentType, Guid? id,
            ILogger log)
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

                var queryParameters = new QueryParameters(contentType.ToLower(), id);

                //Could move in to helper class
                var queryToExecute = this.BuildQuery(queryParameters, req.Path.Value);

                var recordsResult = await ExecuteCypherQuery(queryToExecute.Query, log);

                var serializedResult = JsonConvert.SerializeObject(recordsResult);

                if (recordsResult == null || !recordsResult.Any())
                    return new NotFoundObjectResult(null);

                log.LogInformation("request has successfully been completed with results");

                return new OkObjectResult(_jsonFormatHelper.FormatResponse(recordsResult, queryToExecute.RequestType));
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

        private async Task<IEnumerable<IRecord>> ExecuteCypherQuery(string query, ILogger log)
        {
            log.LogInformation($"Attempting to query neo4j with the following query: {query}");

            try
            {
                return await _graphDatabase.Run(new GenericCypherQuery(query));
            }
            catch (Exception ex)
            {
                throw ApiFunctionException.InternalServerError("Unable To run query", ex);
            }
        }

        private ExecuteQuery BuildQuery(QueryParameters queryParameters, string requestPath)
        {
            if (!queryParameters.Id.HasValue)
            {
                //GetAll Query
                return new ExecuteQuery(string.Format(contentGetAllCypher, MapContentTypeToNamespace(queryParameters.ContentType)), RequestType.GetAll);
            }
            else
            {
                if (_contentTypeMapSettings.CurrentValue.OverrideUri)
                {
                    //Change to use request path when neo has been updated
                    var uri = $"http://nationalcareers.service.gov.uk/{queryParameters.ContentType}/{queryParameters.Id}";
                    return new ExecuteQuery(string.Format(contentByIdCypher, uri), RequestType.GetById);
                }
                else
                {
                    return new ExecuteQuery(string.Format(contentByIdCypher, requestPath), RequestType.GetById);
                }
            }
        }

        private string MapContentTypeToNamespace(string contentType)
        {
            _contentTypeMapSettings.CurrentValue.ContentTypeMap.TryGetValue(contentType.ToLower(), out string mappedValue);

            if (string.IsNullOrWhiteSpace(mappedValue))
            {
                throw ApiFunctionException.BadRequest($"Content Type {contentType} is not mapped in AppSettings");
            }

            return mappedValue;
        }
    }
}
