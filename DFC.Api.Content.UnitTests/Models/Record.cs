using System.Collections.Generic;
using DFC.Api.Content.Interfaces;

namespace DFC.Api.Content.UnitTests.Models
{
    // Class taken from Neo4J Driver library and made public for testing only
    internal class Record : IRecord
    {
        public object this[int index] => Values[Keys[index]];
        public object this[string key] => Values[key];

        public IReadOnlyDictionary<string, object> Values { get; }
        public IReadOnlyList<string> Keys { get; }

        public Record(string[] keys, object[] values)
        {
            var valueKeys = new Dictionary<string, object>();

            for (var i = 0; i < keys.Length; i++)
            {
                valueKeys.Add(keys[i], values[i]);
            }
            Values = valueKeys;
            Keys = keys;
        }
    }
}