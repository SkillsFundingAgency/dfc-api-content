using DFC.Api.Content.Enums;
using Neo4j.Driver;
using System.Collections.Generic;

namespace DFC.Api.Content.Helpers
{
    public interface IJsonFormatHelper
    {
        object FormatResponse(IEnumerable<IRecord> recordsResult, RequestType type);
    }
}
