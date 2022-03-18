using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using DFC.Api.Content.Enums;
using DFC.Api.Content.Exceptions;
using DFC.Api.Content.Helpers;
using DFC.Api.Content.Interfaces;
using DFC.Api.Content.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace DFC.Api.Content.Function
{
    public class Execute
    {
        private readonly IOptionsMonitor<ContentApiOptions> _contentApiOptions;
        private readonly IDataSourceProvider _dataSource;
        private readonly IJsonFormatHelper _jsonFormatHelper;
        
        private const string contentByIdCosmosSql = "select * from c where id ='{0}'";
        private const string contentByIdMultiDirectionalCosmosSql = "select * from c where id ='{0}'";
        private const string contentGetAllCosmosSql = "select * from c";
        
        public Execute(IOptionsMonitor<ContentApiOptions> contentApiOptions, IDataSourceProvider dataSource, IJsonFormatHelper jsonFormatHelper)
        {
            _contentApiOptions = contentApiOptions ?? throw new ArgumentNullException(nameof(contentApiOptions));
            _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
            _jsonFormatHelper = jsonFormatHelper ?? throw new ArgumentNullException(nameof(jsonFormatHelper));
        }

        [FunctionName("Execute")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Execute/{contentType}/{id:guid?}/{multiDirectional:bool?}")]
            HttpRequest req,
            string contentType,
            Guid? id,
            bool? multiDirectional,
            ILogger log)
        {
            var hasApimHeader = req.Headers.TryGetValue("X-Forwarded-APIM-Url", out var headerValue);
            
            try
            {
                var environment = Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT");
                log.LogInformation("Function has been triggered in {Environment} environment", environment);

                if (string.IsNullOrWhiteSpace(contentType))
                {
                    throw ApiFunctionException.BadRequest("Required parameter contentType not found in path.");
                }

                var queryParameters = new QueryParameters(contentType.ToLower(), id);
                var reqPath = req.Path.Value.ToLower()
                    .Replace("/false", string.Empty)
                    .Replace("/true", string.Empty);

                var itemUri = hasApimHeader ? $"{headerValue}{_contentApiOptions.CurrentValue.Action}/api/Execute/{contentType.ToLower()}/{id}".ToLower()
                    : $"{_contentApiOptions.CurrentValue.Scheme}://{req.Host.Value}{reqPath}".ToLower();

                var apiHost = hasApimHeader ? $"{headerValue}{_contentApiOptions.CurrentValue.Action}/api/Execute"
                    : $"{_contentApiOptions.CurrentValue.Scheme}://{req.Host.Value}/api/execute".ToLower();

                var queryToExecute = BuildQuery(queryParameters, itemUri, multiDirectional ?? false);
                var recordsResult = await ExecuteQuery(queryToExecute.Query, log);

                if (!recordsResult.Any())
                {
                    return new NotFoundObjectResult("Resource not found");
                }

                log.LogInformation("Request has successfully been completed with results");

                SetContentTypeHeader(req);
                return new OkObjectResult(_jsonFormatHelper.FormatResponse(recordsResult, queryToExecute.RequestType, apiHost, multiDirectional ?? false));
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

        private static void SetContentTypeHeader(HttpRequest req)
        {
            var headers = req.HttpContext.Response.Headers;
            
            headers.Remove("content-type");
            headers.Add("content-type", "application/hal+json");
        }

        private async Task<IList<IRecord>> ExecuteQuery(string query, ILogger log)
        {
            log.LogInformation("Attempting to query data source with the following query: {Query}", query);

            try
            {
                return (await _dataSource.Run("target", new GenericQuery(query))).ToList();
            }
            catch (Exception ex)
            {
                throw ApiFunctionException.InternalServerError("Unable To run query", ex);
            }
        }

        private ExecuteQuery BuildQuery(QueryParameters queryParameters, string requestPath, bool multiDirectional)
        {
            if (!queryParameters.Id.HasValue)
            {
                //GetAll Query
                return new ExecuteQuery(contentGetAllCosmosSql, RequestType.GetAll);
            }
            
            var uri = GenerateUri(queryParameters.ContentType, queryParameters.Id.Value, requestPath);

            var baseQuery = multiDirectional ? contentByIdMultiDirectionalCosmosSql : contentByIdCosmosSql;
            return new ExecuteQuery(string.Format(baseQuery, uri), RequestType.GetById);
        }

        private string GenerateUri(string contentType, Guid id, string requestPath)
        {
            _contentApiOptions.CurrentValue.ContentTypeUriMap.TryGetValue(contentType.ToLower(), out var mappedValue);
            
            return string.IsNullOrWhiteSpace(mappedValue) ? requestPath : string.Format(mappedValue, id);
        }
    }
}
