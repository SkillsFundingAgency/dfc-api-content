using System.Collections.Generic;
using DFC.Api.Content.Enums;

namespace DFC.Api.Content.Interfaces
{
    public interface IJsonFormatHelper
    {
        object FormatResponse(List<Dictionary<string, object>> records, RequestType type, bool multiDirectional);

        Dictionary<string, object> ExpandIncomingLinksToContItems(Dictionary<string, object> record, bool multiDirectional);

        Dictionary<string, object> SafeCastToDictionary(object value);

        Dictionary<string, object> AddMultiDirectionalProperty(Dictionary<string, object> record);

        Dictionary<string, object> BuildSingleResponse(Dictionary<string, object> record, bool multiDirectional);
    }
}
