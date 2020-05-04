using DFC.Api.Content.Enums;
using Neo4j.Driver;
using System;
using System.Collections.Generic;
using System.Text;

namespace DFC.Api.Content.Helpers
{
    public interface IJsonFormatHelper
    {
        string FormatResponse(IEnumerable<IRecord> recordsResult, RequestType type);
    }
}
