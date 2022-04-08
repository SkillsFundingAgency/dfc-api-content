using System.Collections.Generic;
using DFC.Api.Content.Enums;

namespace DFC.Api.Content.Interfaces
{
    public interface IJsonFormatHelper
    {
        object FormatResponse(List<Dictionary<string, object>> records, RequestType type, bool multiDirectional);

        Dictionary<string, object> SafeCastToDictionary(object value);

        List<Dictionary<string, object>> SafeCastToList(object value);

        Dictionary<string, object> BuildSingleResponse(Dictionary<string, object> record, bool multiDirectional);
    }
}
