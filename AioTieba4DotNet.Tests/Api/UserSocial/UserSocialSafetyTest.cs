using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Api.UserSocial;

[TestClass]
public class UserSocialSafetyTest : TestBase
{
    [TestMethod]
    [TestCategory("Live")]
    public void SafeTargetPortraitFixture_IsExplicitlyRequired_ForLiveFollowMutations()
    {
        var portrait = RequireSafeTargetPortraitFixture(nameof(SafeTargetPortraitFixture_IsExplicitlyRequired_ForLiveFollowMutations));

        Console.WriteLine($"safeTargetPortrait={portrait}");
    }

    [TestMethod]
    [TestCategory("Live")]
    public void SafeTargetUserIdFixture_IsExplicitlyRequired_ForLiveBlacklistAndFanMutations()
    {
        var userId = RequireSafeTargetUserIdFixture(nameof(SafeTargetUserIdFixture_IsExplicitlyRequired_ForLiveBlacklistAndFanMutations));

        Console.WriteLine($"safeTargetUserId={userId}");
    }
}
