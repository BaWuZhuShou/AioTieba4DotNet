namespace AioTieba4DotNet.Models.Users;

/// <summary>
///     虚拟形象
/// </summary>
public class VirtualImagePf
{
    /// <summary>
    ///     是否启用
    /// </summary>
    public bool Enabled;

    /// <summary>
    ///     状态
    /// </summary>
    public string State = "";
   /// <summary>
    ///     转换为字符串
    /// </summary>
    /// <returns>摘要</returns>
    public override string ToString()
    {
        return $"{nameof(Enabled)}: {Enabled}, {nameof(State)}: {State}";
    }
}
