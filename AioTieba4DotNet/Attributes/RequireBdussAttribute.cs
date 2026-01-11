namespace AioTieba4DotNet.Attributes;

/// <summary>
///     标记该 API 需要 BDUSS 才能调用
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireBdussAttribute : Attribute
{
}
