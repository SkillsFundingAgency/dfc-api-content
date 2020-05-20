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
using DFC.Api.Content.Helpers;
using DFC.Api.Content.Enums;
using DFC.Api.Content.Models;

namespace DFC.ServiceTaxonomy.ApiFunction.Function
{
    public class Execute
    {
        private readonly IOptionsMonitor<ContentTypeSettings> _contentTypeSettings;
        private readonly IGraphDatabase _graphDatabase;
        private readonly IJsonFormatHelper _jsonFormatHelper;
        private const string contentByIdCypher = "MATCH (s {{uri:'{0}'}}) optional match(s)-[r]->(d) with s, {{href:d.uri, type:'GET', title:d.skos__prefLabel, relationship:type(r), dynamicKey:reduce(lab = '', n IN labels(d) | case n WHEN 'Resource' THEN lab + '' WHEN 'skos__Concept' THEN lab +  '' WHEN 'esco__MemberConcept' THEN lab + '' ELSE lab +  n END), rel:labels(d)}} as destinationUris with s, apoc.map.fromValues([destinationUris.dynamicKey, {{href: destinationUris.href, relationship:destinationUris.relationship }}]) as map with s, collect(map) as links with s,links,{{ data: properties(s)}} as sourceNodeWithOutgoingRelationships return {{data:sourceNodeWithOutgoingRelationships.data, _links:links}}";

        private const string contentGetAllCypher = "MATCH (s) where ANY(l in labels(s) where toLower(l) =~ '{0}') optional match(s)-[r]->(d) with s, {{href:d.uri, type:'GET', title:d.skos__prefLabel, dynamicKey:reduce(lab = '', n IN labels(d) | case n WHEN 'Resource' THEN lab + '' WHEN 'skos__Concept' THEN lab +  '' WHEN 'esco__MemberConcept' THEN lab + '' ELSE lab +  n END), relationship:type(r), rel:labels(d)}} as destinationUris with s, apoc.map.fromValues([destinationUris.dynamicKey, {{href: destinationUris.href, relationship:destinationUris.relationship }}]) as map with s, collect(map) as links with s,links,{{ data: properties(s)}} as sourceNodeWithOutgoingRelationships return {{data:sourceNodeWithOutgoingRelationships.data, _links:links}}";

        public Execute(IOptionsMonitor<ContentTypeSettings> ContentTypeNameMapSettings, IGraphDatabase graphDatabase, IJsonFormatHelper jsonFormatHelper)
        {
            _contentTypeSettings = ContentTypeNameMapSettings ?? throw new ArgumentNullException(nameof(ContentTypeNameMapSettings));
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

                if (string.IsNullOrWhiteSpace(contentType))
                {
                    throw ApiFunctionException.BadRequest($"Required parameter contentType not found in path.");
                }

                var queryParameters = new QueryParameters(contentType.ToLower(), id);

                //Could move in to helper class
                // Scheme from configuration to allow local debug without HTTPS and certificates
                var queryToExecute = this.BuildQuery(queryParameters, $"{_contentTypeSettings.CurrentValue.Scheme}://{req.Host.Value}{req.Path.Value}");

                var recordsResult = await ExecuteCypherQuery(queryToExecute.Query, log);

                if (recordsResult == null || !recordsResult.Any())
                    return new NotFoundObjectResult(null);

                log.LogInformation("request has successfully been completed with results");

                SetContentTypeHeader(req);
                return new OkObjectResult(_jsonFormatHelper.FormatResponse(recordsResult, queryToExecute.RequestType));
            }
            catch (ApiFunctionException e)
            {
                log.LogError(e.ToString());
                return e.ActionResult ?? new InternalServerErrorResult();
            }
            catch (Exception e)
            {
                log.LogError(e.ToString());
                return new InternalServerErrorResult();
            }
        }

        private static void SetContentTypeHeader(HttpRequest req)
        {
            req.HttpContext.Response.Headers.Remove("content-type");
            req.HttpContext.Response.Headers.Add("content-type", "application/hal+json");
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
                return new ExecuteQuery(string.Format(contentGetAllCypher, MapContentTypeToNamespace(queryParameters.ContentType).ToLowerInvariant()), RequestType.GetAll);
            }
            else
            {
                var uri = GenerateUri(queryParameters.ContentType, queryParameters.Id.Value, requestPath);
                return new ExecuteQuery(string.Format(contentByIdCypher, uri), RequestType.GetById);

            }
        }

        private string GenerateUri(string contentType, Guid id, string host)
        {

            _contentTypeSettings.CurrentValue.ContentTypeUriMap.TryGetValue(contentType.ToLower(), out string? mappedValue);

            if (string.IsNullOrWhiteSpace(mappedValue))
            {
                return host;
            }

            return string.Format(mappedValue, id);
        }

        private string MapContentTypeToNamespace(string contentType)
        {
            _contentTypeSettings.CurrentValue.ContentTypeNameMap.TryGetValue(contentType.ToLower(), out string? mappedValue);

            if (string.IsNullOrWhiteSpace(mappedValue))
            {
                return contentType;
            }

            return mappedValue;
        }
    }
}
