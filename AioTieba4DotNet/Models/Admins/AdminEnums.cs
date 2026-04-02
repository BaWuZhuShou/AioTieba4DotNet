using System;

using System.Diagnostics.CodeAnalysis;

namespace AioTieba4DotNet.Models.Admins;

/// <summary>
///     吧务后台搜索类型
/// </summary>
public enum BawuSearchType
{
    /// <summary>
    ///     按被操作用户筛选
    /// </summary>
    User = 0,

    /// <summary>
    ///     按操作者筛选
    /// </summary>
    Operator = 1
}

/// <summary>
///     吧务类型
/// </summary>
public enum BawuType
{
    /// <summary>
    ///     小吧主
    /// </summary>
    Manager,

    /// <summary>
    ///     图片小编
    /// </summary>
    ImageEditor,

    /// <summary>
    ///     语音小编
    /// </summary>
    VoiceEditor
}

/// <summary>
///     已分配吧务权限
/// </summary>
[SuppressMessage("Naming", "S2342:Enumeration type names should comply with a naming convention",
    Justification = "The public flags enum name is part of the existing consumer-facing admin contract and is retained for compatibility.")]
[Flags]
public enum BawuPermType
{
    /// <summary>
    ///     无权限
    /// </summary>
    None = 0,

    /// <summary>
    ///     解除封禁
    /// </summary>
    Unblock = 1,

    /// <summary>
    ///     处理封禁申诉
    /// </summary>
    UnblockAppeal = 2,

    /// <summary>
    ///     恢复删帖
    /// </summary>
    Recover = 4,

    /// <summary>
    ///     处理删帖申诉
    /// </summary>
    RecoverAppeal = 8,

    /// <summary>
    ///     所有权限
    /// </summary>
    All = Unblock | UnblockAppeal | Recover | RecoverAppeal
}
