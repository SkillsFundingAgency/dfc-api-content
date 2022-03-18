using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DFC.Api.Content.Interfaces;
using IRecord = DFC.Api.Content.Interfaces.IRecord;

namespace DFC.Api.Content.Models
{
    [ExcludeFromCodeCoverage]
    public class GenericQuery : IQuery<IRecord>
    {
        private string QueryToRun { get; }
        public GenericQuery(string query) => QueryToRun = query;

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
                CheckIsValid();
                return new Query(QueryToRun);
            }
        }

        public IRecord ProcessRecord(IRecord record)
        {
            return record;
        }

        public bool CheckIsValid()
        {
            return true;
        }
    }
}
