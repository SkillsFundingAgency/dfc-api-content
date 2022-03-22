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
using Microsoft.Extensions.Options;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace DFC.Api.Content.Function
{
    public class Execute
    {
        private readonly IOptionsMonitor<ContentApiOptions> _contentApiOptions;
        private readonly IDataSourceProvider _dataSource;
        private readonly IJsonFormatHelper _jsonFormatHelper;

        public Execute(IOptionsMonitor<ContentApiOptions> contentApiOptions, IDataSourceProvider dataSource, IJsonFormatHelper jsonFormatHelper)
        {
            _contentApiOptions = contentApiOptions ?? throw new ArgumentNullException(nameof(contentApiOptions));
            _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
            _jsonFormatHelper = jsonFormatHelper ?? throw new ArgumentNullException(nameof(jsonFormatHelper));
        }

        [FunctionName("Execute")]
        public async Task<IActionResult> Run(
            [HttpTrigger(
                AuthorizationLevel.Anonymous,
                "get",
                Route = "Execute/{contentType}/{id:guid?}/{publishState?}/{multiDirectional:bool?}")]
            HttpRequest request,
            string contentType,
            Guid? id,
            ILogger log,
            bool? multiDirectional,
            string publishState = "")
        {
            MaintainBackwardsCompatibilityWithPath(ref publishState, ref multiDirectional);
            SetDefaultStateIfEmpty(ref publishState, request);
            
            var hasApimHeader = request.Headers.TryGetValue("X-Forwarded-APIM-Url", out var headerValue);
            
            try
            {
                var environment = Environment.GetEnvironmentVariable("AZURE_FUNCTIONS_ENVIRONMENT");
                log.LogInformation("Function has been triggered in {Environment} environment", environment);

                if (string.IsNullOrWhiteSpace(contentType))
                {
                    throw ApiFunctionException.BadRequest("Required parameter contentType not found in path.");
                }

                var ids = new List<Guid?>();
                if (id != null)
                {
                    ids.Add(id);
                }

                var queryParameters = new QueryParameters(contentType.ToLower(), ids);
                var queryToExecute = BuildQuery(queryParameters, publishState);
                var recordsResult = await ExecuteQuery(queryToExecute, log);

                if (!recordsResult.Any())
                {
                    return new NotFoundObjectResult("Resource not found");
                }

                log.LogInformation("Request has successfully been completed with results");

                SetContentTypeHeader(request);
                
                var apiHost = hasApimHeader ? $"{headerValue}{_contentApiOptions.CurrentValue.Action}/api/Execute"
                    : $"{_contentApiOptions.CurrentValue.Scheme}://{request.Host.Value}/api/execute".ToLower();
                
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
        
        private static void MaintainBackwardsCompatibilityWithPath(ref string publishState, ref bool? multiDirectional)
        {
            if (!bool.TryParse(publishState, out var multiDirectionalOutput)) return;

            multiDirectional = multiDirectionalOutput;
            publishState = string.Empty;
        }
        
        private static void SetDefaultStateIfEmpty(ref string publishState, HttpRequest request)
        {
            if (!string.IsNullOrEmpty(publishState)) return;
            
            publishState = GetDefaultPublishState(request.Host.Host);
        }
        
        private static string GetDefaultPublishState(string host)
        {
            const string previewPublishState = "preview";
            const string previewPublishStateAlt = "draft";
            const string publishedPublishState = "publish";
            
            if (host.Contains(previewPublishState, StringComparison.InvariantCultureIgnoreCase) ||
                host.Contains(previewPublishStateAlt, StringComparison.InvariantCultureIgnoreCase))
            {
                return previewPublishState;
            }
            
            return publishedPublishState;
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
                return (await _dataSource.Run(new GenericQuery(query.QueryText, query.ContentType, query.PublishState))).ToList();
            }
            catch (Exception ex)
            {
                throw ApiFunctionException.InternalServerError("Unable to run query", ex);
            }
        }

        private static ExecuteQuery BuildQuery(QueryParameters queryParameters, string publishState)
        {   
            const string contentByIdCosmosSql = "select * from c where c.id ='{0}'";
            const string contentGetAllCosmosSql = "select * from c";
            
            if (!queryParameters.Ids.Any())
            {
                return new ExecuteQuery(contentGetAllCosmosSql, RequestType.GetAll, queryParameters.ContentType, publishState);
            }
            
            return new ExecuteQuery(
                string.Format(contentByIdCosmosSql, queryParameters.Ids.First()!.Value),
                RequestType.GetById,
                queryParameters.ContentType,
                publishState);
        }
    }
}