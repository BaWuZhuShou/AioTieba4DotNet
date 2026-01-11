using System.Collections;

namespace AioTieba4DotNet.Api.Entities;

/// <summary>
///     内容列表的泛型基类
///     约定取内容的通用接口
/// </summary>
/// <typeparam name="T">内容类型</typeparam>
public class Containers<T> : IReadOnlyList<T>
{
    // Python中用field初始化，C#类似的做法如下
    private readonly List<T> _objs = [];

    /// <summary>
    ///     构造函数
    /// </summary>
    /// <param name="objs">对象列表</param>
    public Containers(List<T> objs)
    {
        _objs.AddRange(objs);
    }

    /// <summary>
    ///     构造函数
    /// </summary>
    /// <param name="collection">对象集合</param>
    public Containers(IEnumerable<T>? collection)
    {
        if (collection != null)
            _objs.AddRange(collection);
    }

    /// <summary>
    ///     只读对象列表
    /// </summary>
    public IReadOnlyList<T> Objs => _objs;

    /// <summary>
    ///     获取枚举器
    /// </summary>
    /// <returns>枚举器</returns>
    public IEnumerator<T> GetEnumerator()
    {
        return _objs.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _objs.GetEnumerator();
    }

    /// <summary>
    ///     索引访问器
    /// </summary>
    /// <param name="index">索引</param>
    public T this[int index] => _objs[index];

    /// <summary>
    ///     元素数量
    /// </summary>
    public int Count => _objs.Count;

    /// <summary>
    ///     添加元素
    /// </summary>
    /// <param name="item">元素</param>
    protected void Add(T item)
    {
        _objs.Add(item);
    }

    /// <summary>
    ///     设置元素（未实现）
    /// </summary>
    /// <param name="index">索引</param>
    /// <param name="value">值</param>
    /// <exception cref="NotImplementedException"></exception>
    public void SetItem(int index, T value)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     移除元素（未实现）
    /// </summary>
    /// <param name="index">索引</param>
    /// <exception cref="NotImplementedException"></exception>
    public void RemoveAt(int index)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     是否包含元素
    /// </summary>
    /// <returns>True 如果包含元素</returns>
    public bool Any()
    {
        return _objs.Count > 0;
    }
}
