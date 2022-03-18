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
        private CosmosClient CosmosClient { get; }

        public CosmosDbService(string connectionString)
        {
            CosmosClient = new CosmosClient(connectionString);
        }
        
        public async Task<List<Dictionary<string, object>>> Run(GenericQuery query)
        {
            var container = GetContainer("published");
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
        
        private Container GetContainer(string databaseName)
        {
            var database = CosmosClient.GetDatabase("dev");
            return database.GetContainer(databaseName);
        }
    }
}