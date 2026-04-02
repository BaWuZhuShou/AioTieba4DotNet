using System.Diagnostics.CodeAnalysis;
using AioTieba4DotNet.Models.Shared;

namespace AioTieba4DotNet.Api.Login.Entities;

/// <summary>
///     登录用户信息
/// </summary>
[SuppressMessage("Minor Code Smell", "S2094:Classes should not be empty",
    Justification =
        "The login API keeps this semantic subtype so login-specific user payloads remain distinguishable while reusing the shared base model.")]
internal sealed class UserInfoLogin : UserInfo
{
}
