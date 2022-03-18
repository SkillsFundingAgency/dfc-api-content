using System.Collections.Generic;
using DFC.Api.Content.Models;

namespace DFC.Api.Content.Interfaces
{
    public interface IQuery<out TRecord>
    {
        List<string> ValidationErrors();

        Query Query { get; }

        TRecord ProcessRecord(IRecord record);

        bool CheckIsValid();
    }
}