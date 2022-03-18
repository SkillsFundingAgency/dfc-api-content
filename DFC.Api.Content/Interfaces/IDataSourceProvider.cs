using System.Collections.Generic;
using System.Threading.Tasks;
using DFC.Api.Content.Models;

namespace DFC.Api.Content.Interfaces
{
    public interface IDataSourceProvider
    {
        public Task<List<Dictionary<string, object>>> Run(GenericQuery query);
    }
}