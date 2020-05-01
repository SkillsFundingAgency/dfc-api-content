using System;
using System.Collections.Generic;
using System.Text;

namespace DFC.Api.Content.Helpers
{
    public interface IJsonFormatHelper
    {
        string ReplaceNamespaces(object input);
        string CreateSingleRootObject(object input);
    }
}
