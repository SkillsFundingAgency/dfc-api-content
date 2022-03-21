using System.Diagnostics.CodeAnalysis;
using DFC.Api.Content.Interfaces;
using IRecord = DFC.Api.Content.Interfaces.IRecord;

namespace DFC.Api.Content.Models
{
    [ExcludeFromCodeCoverage]
    public class GenericQuery : IQuery<IRecord>
    {
        public string QueryText { get; }
        public string ContentType { get; }
        
        public string State { get; }
        
        public GenericQuery(string queryText, string contentType, string state)
        {
            QueryText = queryText;
            ContentType = contentType;
            State = state;
        }
    }
}
