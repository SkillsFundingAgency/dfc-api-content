using DFC.Api.Content.Enums;
using System.Collections.Generic;
using IRecord = DFC.Api.Content.Interfaces.IRecord;

namespace DFC.Api.Content.Helpers
{
    public interface IJsonFormatHelper
    {
        object FormatResponse(IList<IRecord> recordsResult, RequestType type, string apiHost, bool multiDirectional);
    }
}
