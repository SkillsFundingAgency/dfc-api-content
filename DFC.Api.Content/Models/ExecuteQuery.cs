using System.Collections.Generic;

namespace DFC.Api.Content.Models
{
    public class ExecuteQuery
    {
        public ExecuteQuery(
            string queryText,
            Dictionary<string, object> parameters)
        {
            QueryText = queryText;
            Parameters = parameters;
        }

        public string QueryText { get; }
        
        public Dictionary<string, object> Parameters { get; }
    }
}