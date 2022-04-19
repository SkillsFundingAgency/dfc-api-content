using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using DFC.Api.Content.Interfaces;
using DFC.Api.Content.Models;
using Microsoft.Azure.Cosmos;

namespace DFC.Api.Content.Services
{
    [ExcludeFromCodeCoverage]
    public class CosmosDbService : IDataSourceProvider
    {
        private CosmosClient PreviewCosmosClient { get; }
        private string? PreviewDatabaseName { get; }
        private string? PreviewContainerName { get; }
        
        private CosmosClient PublishedCosmosClient { get; }
        private string? PublishedDatabaseName { get; }
        private string? PublishedContainerName { get; }
        
        public CosmosDbService(
            string? previewConnectionString,
            string? previewDatabaseName,
            string? previewContainerName,
            string? publishedConnectionString,
            string? publishedDatabaseName,
            string? publishedContainerName
            )
        {
            PreviewCosmosClient = new CosmosClient(previewConnectionString);
            PreviewDatabaseName = previewDatabaseName;
            PreviewContainerName = previewContainerName;
            
            PublishedCosmosClient = new CosmosClient(publishedConnectionString);
            PublishedDatabaseName = publishedDatabaseName;
            PublishedContainerName = publishedContainerName;
        }
        
        public async Task<List<Dictionary<string, object>>> Run(GenericQuery query)
        {
            var container = GetContainer(query.PublishState);
            return await GetItemFromDatabase(container, query.QueryText, query.ContentType, query.Parameters);
        }
        
        private Container GetContainer(string publishState)
        {
            return IsPreview(publishState) ?
                PreviewCosmosClient.GetDatabase(PreviewDatabaseName).GetContainer(PreviewContainerName)
                : PublishedCosmosClient.GetDatabase(PublishedDatabaseName).GetContainer(PublishedContainerName);
        }
        
        private static async Task<List<Dictionary<string, object>>> GetItemFromDatabase(
            Container container,
            string queryText,
            string contentType,
            Dictionary<string, object> parameters)
        {
            var queryDefinition = new QueryDefinition(queryText);

            foreach (var parameter in parameters)
            {
                queryDefinition = queryDefinition.WithParameter(parameter.Key, parameter.Value);
            }
            
            var iteratorLoop = container.GetItemQueryIterator<Dictionary<string, object>>(
                queryDefinition,
                requestOptions: new QueryRequestOptions
                {
                    PartitionKey = new PartitionKey(contentType)
                });

            var result = await iteratorLoop.ReadNextAsync();
            return result.Resource.ToList();
        }

        private static bool IsPreview(string publishState)
        {
            const string previewPublishState = "preview";
            return publishState.Equals(previewPublishState, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}