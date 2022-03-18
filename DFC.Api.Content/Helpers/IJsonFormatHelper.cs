using DFC.Api.Content.Enums;
using System.Collections.Generic;

namespace DFC.Api.Content.Helpers
{
    public interface IJsonFormatHelper
    {
        object FormatResponse(List<Dictionary<string, object>> records, RequestType type, string apiHost, bool multiDirectional);
    }
}
