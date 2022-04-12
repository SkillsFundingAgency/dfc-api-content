using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DFC.Api.Content.Exceptions;
using DFC.Api.Content.Interfaces;
using DFC.Api.Content.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace DFC.Api.Content.Function
{
    public class BaseFunction
    {
        // ReSharper disable once InconsistentNaming
        protected IDataSourceProvider _dataSource = null!;
        // ReSharper disable once InconsistentNaming
        protected IJsonFormatHelper _jsonFormatHelper = null!;
        
        protected static (string ContentType, Guid Id, int ParentPosition) GetContentTypeIdAndPosition(string uri, int parentPosition)
        {
            var pathOnly = uri.StartsWith("http") ? new Uri(uri, UriKind.Absolute).AbsolutePath : uri;
            pathOnly = pathOnly.ToLower().Replace("/api/execute", string.Empty);
            
            var uriParts = pathOnly.Trim('/').Split('/');
            var contentType = uriParts[0].ToLower();
            var id = Guid.Parse(uriParts[1]);
            
            return (contentType, id, parentPosition);
        }
        
        protected async Task<List<Dictionary<string, object>>> ExecuteQuery(Queries queries, ILogger log)
        {
            try
            {
                var executingQueries = new List<Task<List<Dictionary<string, object>>>>();
                
                foreach (var query in queries.Content)
                {
                    log.LogInformation("Attempting to query data source with the following query: {QueryText}, {ContentType}",
                        query.QueryText,
                        queries.ContentType);   
                    
                    executingQueries.Add(_dataSource.Run(
                        new GenericQuery(query.QueryText, queries.ContentType, queries.PublishState, query.Parameters)));
                }

                var returnList = new List<Dictionary<string, object>>();
                
                foreach (var executingQuery in executingQueries)
                {
                    returnList.AddRange((await executingQuery).ToList());
                }
                
                return returnList;
            }
            catch (Exception ex)
            {
                throw ApiFunctionException.InternalServerError("Unable to run query", ex);
            }
        }
        
        protected static void MaintainBackwardsCompatibilityWithPath(ref string publishState, ref bool? multiDirectional)
        {
            if (!bool.TryParse(publishState, out var multiDirectionalOutput)) return;

            multiDirectional = multiDirectionalOutput;
            publishState = string.Empty;
        }
        
        protected static void SetDefaultStateIfEmpty(ref string publishState, HttpRequest request)
        {
            if (!string.IsNullOrEmpty(publishState)) return;
            
            publishState = GetDefaultPublishState(request.Host.Host);
        }

        protected static void SetContentTypeHeader(HttpRequest req)
        {
            var headers = req.HttpContext.Response.Headers;
            
            headers.Remove("content-type");
            headers.Add("content-type", "application/hal+json");
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
    }
}