using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DFC.Api.Content.Interfaces;
using DFC.Api.Content.Models;
using Microsoft.Azure.Cosmos;

namespace DFC.Api.Content.Services
{
    public class CosmosDbService : IDataSourceProvider
    {
        private CosmosClient PreviewCosmosClient { get; }
        private string PreviewDatabaseName { get; }
        private string PreviewContainerName { get; }
        
        private CosmosClient PublishedCosmosClient { get; }
        private string PublishedDatabaseName { get; }
        private string PublishedContainerName { get; }
        
        public CosmosDbService(
            string previewConnectionString,
            string previewDatabaseName,
            string previewContainerName,
            string publishedConnectionString,
            string publishedDatabaseName,
            string publishedContainerName
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
            var container = GetContainer(query.State);
            return await GetItemFromDatabase(container, query.QueryText, query.ContentType);
        }
        
        private static async Task<List<Dictionary<string, object>>> GetItemFromDatabase(Container container, string queryText, string contentType)
        {
            var iteratorLoop = container.GetItemQueryIterator<Dictionary<string, object>>(
                new QueryDefinition(queryText),
                requestOptions: new QueryRequestOptions { PartitionKey = new PartitionKey(contentType) });

            var result = await iteratorLoop.ReadNextAsync();
            return result.Resource.ToList();
        }
        
        private Container GetContainer(string state)
        {
            return state == "preview" ?
                PreviewCosmosClient.GetDatabase(PreviewDatabaseName).GetContainer(PreviewContainerName)
                : PublishedCosmosClient.GetDatabase(PublishedDatabaseName).GetContainer(PublishedContainerName);
        }
    }
}