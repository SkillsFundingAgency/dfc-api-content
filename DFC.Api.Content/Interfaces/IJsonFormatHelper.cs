using System.Collections.Generic;
using DFC.Api.Content.Enums;

namespace DFC.Api.Content.Interfaces
{
    public interface IJsonFormatHelper
    {
        object FormatResponse(List<Dictionary<string, object>> records, RequestType type, string apiHost, bool multiDirectional);
    }
}
