using DFC.ServiceTaxonomy.Neo4j.Queries.Interfaces;
using Neo4j.Driver;
using System.Collections.Generic;
using DFC.ServiceTaxonomy.Neo4j.Queries;

namespace DFC.Api.Content.Models.Cypher
{
    public class GenericCypherQuery : IQuery<IRecord>
    {
        public string QueryToRun { get; set; }
        public GenericCypherQuery(string query) => QueryToRun = query;

        public List<string> ValidationErrors()
        {
            var validationErrors = new List<string>();

            if (string.IsNullOrEmpty(QueryToRun))
            {
                validationErrors.Add("No query specified to run.");
            }

            return validationErrors;
        }

        public Query Query
        {
            get
            {
                this.CheckIsValid();
                return new Query(QueryToRun);
            }
        }

        public IRecord ProcessRecord(IRecord record)
        {
            return record;
        }
    }
}
