namespace AioTieba4DotNet.Models.Users;

/// <summary>
///     虚拟形象
/// </summary>
public class VirtualImagePf
{
    /// <summary>
    ///     Gets or sets a value that indicates whether the virtual image is enabled.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    ///     Gets or sets the virtual image state.
    /// </summary>
    public string State { get; set; } = string.Empty;

    /// <summary>
    ///     转换为字符串
    /// </summary>
    /// <returns>摘要</returns>
    public override string ToString()
    {
        return $"{nameof(Enabled)}: {Enabled}, {nameof(State)}: {State}";
    }
}
