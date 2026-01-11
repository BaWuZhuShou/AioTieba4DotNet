namespace AioTieba4DotNet.Core;

/// <summary>
///     提供线程安全的异步初始化机制（Async Double-Check Locking）
/// </summary>
/// <typeparam name="T">资源类型</typeparam>
/// <param name="factory">异步获取资源的工厂方法</param>
public class AsyncInit<T>(Func<Task<T>> factory)
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private T? _value;

    /// <summary>
    ///     获取当前是否已初始化
    /// </summary>
    public bool IsValueCreated { get; private set; }

    /// <summary>
    ///     获取资源。如果尚未初始化，则调用工厂方法进行初始化。
    /// </summary>
    /// <returns>资源实例</returns>
    public async Task<T> GetAsync()
    {
        // 1. 快速检查
        if (IsValueCreated) return _value!;

        // 2. 加锁
        await _lock.WaitAsync();
        try
        {
            // 3. 双重检查
            if (IsValueCreated) return _value!;

            // 4. 执行初始化
            _value = await factory();
            IsValueCreated = true;
            return _value;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    ///     直接设置值（通常用于预加载或恢复状态）
    /// </summary>
    /// <param name="value">要设置的值</param>
    public void SetValue(T value)
    {
        _value = value;
        IsValueCreated = true;
    }
}
