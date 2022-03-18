using System.Collections.Generic;

namespace DFC.Api.Content.Interfaces
{
    /// <summary>A record contains ordered key and value pairs</summary>
    public interface IRecord
    {
        /// <summary>Gets the value at the given index.</summary>
        /// <param name="index">The index</param>
        /// <returns>The value specified with the given index.</returns>
        object this[int index] { get; }

        /// <summary>Gets the value specified by the given key.</summary>
        /// <param name="key">The key</param>
        /// <returns>the value specified with the given key.</returns>
        object this[string key] { get; }

        /// <summary>
        /// Gets the key and value pairs in a <see cref="T:System.Collections.Generic.IReadOnlyDictionary`2" />.
        /// </summary>
        IReadOnlyDictionary<string, object> Values { get; }

        /// <summary>
        /// Gets the keys in a <see cref="T:System.Collections.Generic.IReadOnlyList`1" />.
        /// </summary>
        IReadOnlyList<string> Keys { get; }
    }
}