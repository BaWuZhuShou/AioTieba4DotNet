using System.Diagnostics.CodeAnalysis;

namespace AioTieba4DotNet.Models.Users;

/// <summary>
///     虚拟形象
/// </summary>
public class VirtualImagePf
{
    /// <summary>
    ///     是否启用
    /// </summary>
    [SuppressMessage("Minor Code Smell", "S1104:Fields should not have public accessibility",
        Justification = "This DTO keeps its original public field shape to preserve the existing consumer-visible contract.")]
    public bool Enabled;

    /// <summary>
    ///     状态
    /// </summary>
    [SuppressMessage("Minor Code Smell", "S1104:Fields should not have public accessibility",
        Justification = "This DTO keeps its original public field shape to preserve the existing consumer-visible contract.")]
    public string State = string.Empty;

    /// <summary>
    ///     转换为字符串
    /// </summary>
    /// <returns>摘要</returns>
    public override string ToString()
    {
        return $"{nameof(Enabled)}: {Enabled}, {nameof(State)}: {State}";
    }
}
