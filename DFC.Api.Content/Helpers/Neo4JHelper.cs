using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DFC.ServiceTaxonomy.ApiFunction.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neo4j.Driver;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace DFC.ServiceTaxonomy.ApiFunction.Helpers
{
    public class Neo4JHelper : INeo4JHelper, IDisposable
    {
        private readonly IAuthToken _authToken = AuthTokens.None;
        private readonly IDriver _neo4JDriver;
        private IResultCursor _resultCursor;

        public Neo4JHelper(IOptionsMonitor<ServiceTaxonomyApiSettings> serviceTaxonomyApiSettings, ILogger log)
        {
            var taxonomyApiSettings = serviceTaxonomyApiSettings?.CurrentValue ?? 
                                        throw new ArgumentNullException(nameof(serviceTaxonomyApiSettings));

            if (string.IsNullOrEmpty(taxonomyApiSettings.Neo4jUrl))
                throw new Exception("Missing Neo4j database uri setting.");
            
            if (string.IsNullOrEmpty(taxonomyApiSettings.Neo4jUser) ||
                string.IsNullOrEmpty(taxonomyApiSettings.Neo4jPassword))
            {
                log.LogWarning("No credentials for Neo4j database in settings, attempting connection without authorization token.");
            }
            else
            {
                _authToken = AuthTokens.Basic(taxonomyApiSettings.Neo4jUser, taxonomyApiSettings.Neo4jPassword);
            }

            _neo4JDriver = GraphDatabase.Driver(taxonomyApiSettings.Neo4jUrl, _authToken);
        }
        
        //todo: create package for DFC.ServiceTaxonomy.Neo4j??
        public async Task<object> ExecuteCypherQueryInNeo4JAsync(string query, IDictionary<string, object> statementParameters)
        {
            IAsyncSession session = _neo4JDriver.AsyncSession();
            try
            {
                return await session.ReadTransactionAsync(async tx =>
                {
                    _resultCursor = await tx.RunAsync(query, statementParameters);
                    return await GetListOfRecordsAsync();
                });
            }
            finally
            {
                await session.CloseAsync();
            }
        }

        private async Task<object> GetListOfRecordsAsync()
        {
            var records =  await _resultCursor.ToListAsync();

            if (records == null || !records.Any())
                return null;

            var neoRecords = records.SelectMany(x => x.Values.Values);

            return neoRecords;
        }

        /// <summary>
        /// Calling this method will discard all remaining records to yield the summary
        /// </summary>
        public async Task<IResultSummary> GetResultSummaryAsync()
        {
            return await _resultCursor.ConsumeAsync();
        }

        public void Dispose()
        {
            _neo4JDriver?.Dispose();
        }
    }
}
