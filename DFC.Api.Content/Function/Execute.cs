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

namespace DFC.ServiceTaxonomy.ApiFunction.Function
{
    public class Execute
    {
        private readonly IOptionsMonitor<ContentTypeMapSettings> _contentTypeMapSettings;
        private readonly IGraphDatabase graphDatabase;

        private const string contentByIdCypher = "MATCH (s {{uri:'{0}'}}) optional match(s)-[r]->(d) with s, {{ href:d.uri, type:'GET', rel:labels(d)}} as destinationUris with {{ properties: properties(s), links: collect(destinationUris)}} as sourceNodeWithOutgoingRelationships return {{ properties:sourceNodeWithOutgoingRelationships.properties, links:sourceNodeWithOutgoingRelationships.links}}";

        private const string contentGetAllCypher = "MATCH (n:{0}) return properties(n);";

        public Execute(IOptionsMonitor<ContentTypeMapSettings> contentTypeMapSettings, IGraphDatabase neo4JHelper)
        {
            _contentTypeMapSettings = contentTypeMapSettings ?? throw new ArgumentNullException(nameof(contentTypeMapSettings));
            graphDatabase = neo4JHelper ?? throw new ArgumentNullException(nameof(neo4JHelper));
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

                var recordsResult = await ExecuteCypherQuery(queryToExecute, log);

                if (recordsResult == null)
                    return new NoContentResult();

                log.LogInformation("request has successfully been completed with results");

                return new OkObjectResult(FormatResponse(recordsResult));
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

        private object FormatResponse(IEnumerable<IRecord> recordsResult)
        {
            if (recordsResult.Count() == 1)
            {
                return ReplaceNamespaces(recordsResult.Select(z => z.Values).FirstOrDefault().Values.FirstOrDefault());
            }

            return ReplaceNamespaces(recordsResult.SelectMany(z => z.Values).Select(y=>y.Value));
        }

        private object ReplaceNamespaces(object input)
        {
            var serializedJson = JsonConvert.SerializeObject(input);

            foreach(var key in _contentTypeMapSettings.CurrentValue.ReversedContentTypeMap.Keys)
            {
                serializedJson = serializedJson.Replace(key, _contentTypeMapSettings.CurrentValue.ReversedContentTypeMap[key]);
            }

            return serializedJson;
        }

        private async Task<IEnumerable<IRecord>> ExecuteCypherQuery(string query, ILogger log)
        {
            log.LogInformation($"Attempting to query neo4j with the following query: {query}");

            try
            {
                return await graphDatabase.Run(new GenericCypherQuery(query));
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
                    //Change to use request path when neo has been updated
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
            _contentTypeMapSettings.CurrentValue.ContentTypeMap.TryGetValue(contentType.ToLower(), out string mappedValue);

            if (string.IsNullOrWhiteSpace(mappedValue))
            {
                throw ApiFunctionException.BadRequest($"Content Type {contentType} is not mapped in AppSettings");
            }

            return mappedValue;
        }
    }
}
