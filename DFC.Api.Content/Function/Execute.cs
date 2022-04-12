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
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace DFC.Api.Content.Function
{
    public class Execute : BaseFunction
    {
        public Execute(IDataSourceProvider dataSource, IJsonFormatHelper jsonFormatHelper)
        {
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
                
                return new OkObjectResult(
                    _jsonFormatHelper.FormatResponse(recordsResult, queryToExecute.RequestType, multiDirectional ?? false));
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
        
        private static ExecuteQueries BuildQuery(QueryParameters queryParameters, string publishState)
        {   
            const string contentByIdCosmosSql = "select * from c where c.id = @id0";
            const string contentGetAllCosmosSql = "select * from c";
            var parameters = new Dictionary<string, object>();
            
            if (!queryParameters.Ids.Any())
            {
                return new ExecuteQueries(
                    new[] { new ExecuteQuery(contentGetAllCosmosSql, parameters) },
                    RequestType.GetAll,
                    queryParameters.ContentType,
                    publishState);
            }
            
            parameters.Add("@id0", queryParameters.Ids.First()!.Value);
            
            return new ExecuteQueries(
                new[] { new ExecuteQuery(contentByIdCosmosSql, parameters) },
                RequestType.GetById,
                queryParameters.ContentType,
                publishState);
        }
    }
}