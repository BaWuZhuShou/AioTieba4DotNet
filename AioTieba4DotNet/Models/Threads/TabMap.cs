using System.Collections;

namespace AioTieba4DotNet.Models.Threads;

/// <summary>
///     表示贴吧分区名到分区 id 的映射。
/// </summary>
public sealed class TabMap : IReadOnlyDictionary<string, int>
{
    private readonly Dictionary<string, int> _map;

    /// <summary>
    ///     Initializes a new instance of the <see cref="TabMap"/> class.
    /// </summary>
    public TabMap() : this([])
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="TabMap"/> class.
    /// </summary>
    /// <param name="map">A tab map collection.</param>
    public TabMap(IEnumerable<KeyValuePair<string, int>> map)
    {
        ArgumentNullException.ThrowIfNull(map);
        _map = map.ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.Ordinal);
    }

    /// <summary>
    ///     获取分区映射。
    /// </summary>
    /// <value>A tab-name to tab-id dictionary.</value>
    public IReadOnlyDictionary<string, int> Map => _map;

    /// <inheritdoc/>
    public int this[string key] => _map[key];

    /// <inheritdoc/>
    public IEnumerable<string> Keys => _map.Keys;

    /// <inheritdoc/>
    public IEnumerable<int> Values => _map.Values;

    /// <inheritdoc/>
    public int Count => _map.Count;

    /// <inheritdoc/>
    public bool ContainsKey(string key)
    {
        return _map.ContainsKey(key);
    }

    /// <inheritdoc/>
    public bool TryGetValue(string key, out int value)
    {
        return _map.TryGetValue(key, out value);
    }

    /// <inheritdoc/>
    public IEnumerator<KeyValuePair<string, int>> GetEnumerator()
    {
        return _map.GetEnumerator();
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
