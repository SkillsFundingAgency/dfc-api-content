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
        
        private const string contentByIdCosmosSql = "select * from c where c.id ='{0}'";
        private const string contentByIdMultiDirectionalCosmosSql = "select * from c where c.id ='{0}'";
        private const string contentGetAllCosmosSql = "select * from c";
        
        public Execute(IOptionsMonitor<ContentApiOptions> contentApiOptions, IDataSourceProvider dataSource, IJsonFormatHelper jsonFormatHelper)
        {
            _contentApiOptions = contentApiOptions ?? throw new ArgumentNullException(nameof(contentApiOptions));
            _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
            _jsonFormatHelper = jsonFormatHelper ?? throw new ArgumentNullException(nameof(jsonFormatHelper));
        }

        [FunctionName("Execute")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Execute/{contentType}/{id:guid?}/{state:string?}/{multiDirectional:bool?}")]
            HttpRequest req,
            string contentType,
            Guid? id,
            string? state,
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
                var queryToExecute = BuildQuery(queryParameters, multiDirectional ?? false, state ?? "published");
                var recordsResult = await ExecuteQuery(queryToExecute, log);

                if (!recordsResult.Any())
                {
                    return new NotFoundObjectResult("Resource not found");
                }

                log.LogInformation("Request has successfully been completed with results");

                SetContentTypeHeader(req);
                
                var apiHost = hasApimHeader ? $"{headerValue}{_contentApiOptions.CurrentValue.Action}/api/Execute"
                    : $"{_contentApiOptions.CurrentValue.Scheme}://{req.Host.Value}/api/execute".ToLower();
                
                return new OkObjectResult(
                    _jsonFormatHelper.FormatResponse(recordsResult, queryToExecute.RequestType, apiHost, multiDirectional ?? false));
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

        private async Task<List<Dictionary<string, object>>> ExecuteQuery(ExecuteQuery query, ILogger log)
        {
            log.LogInformation("Attempting to query data source with the following query: {QueryText}, {ContentType}",
                query.QueryText,
                query.ContentType);

            try
            {
                return (await _dataSource.Run(new GenericQuery(query.QueryText, query.ContentType, query.State))).ToList();
            }
            catch (Exception ex)
            {
                throw ApiFunctionException.InternalServerError("Unable to run query", ex);
            }
        }

        private ExecuteQuery BuildQuery(QueryParameters queryParameters, bool multiDirectional, string state)
        {
            if (!queryParameters.Id.HasValue)
            {
                return new ExecuteQuery(contentGetAllCosmosSql, RequestType.GetAll, queryParameters.ContentType, state);
            }
            
            var baseQuery = multiDirectional ? contentByIdMultiDirectionalCosmosSql : contentByIdCosmosSql;
            return new ExecuteQuery(string.Format(baseQuery, queryParameters.Id.Value), RequestType.GetById, queryParameters.ContentType, state);
        }
    }
}
