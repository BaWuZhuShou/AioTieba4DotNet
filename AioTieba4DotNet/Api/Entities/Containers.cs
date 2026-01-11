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

    // 构造器，可通过传入列表初始化
    public Containers(List<T> objs)
    {
        _objs.AddRange(objs);
    }

    public Containers(IEnumerable<T>? collection)
    {
        if (collection != null)
            _objs.AddRange(collection);
    }

    /// <summary>
    ///     只读对象列表
    /// </summary>
    public IReadOnlyList<T> Objs => _objs;

    // 支持 foreach 遍历（实现 IEnumerable<T>，也可继承 IReadOnlyList<T>）
    public IEnumerator<T> GetEnumerator()
    {
        return _objs.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _objs.GetEnumerator();
    }

    // 支持索引访问
    public T this[int index] => _objs[index];

    // 支持 len（数量）
    public int Count => _objs.Count;

    // 支持添加元素的方法，可根据实际需求公开或保护（这里默认protected）
    protected void Add(T item)
    {
        _objs.Add(item);
    }

    // 不允许 set、del，显式抛出异常
    public void SetItem(int index, T value)
    {
        throw new NotImplementedException();
    }

    public void RemoveAt(int index)
    {
        throw new NotImplementedException();
    }

    // 支持 bool（是否为空）
    public bool Any()
    {
        return _objs.Count > 0;
    }
}
