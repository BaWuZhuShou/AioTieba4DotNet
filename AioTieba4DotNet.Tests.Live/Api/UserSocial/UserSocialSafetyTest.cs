using System;
using AioTieba4DotNet.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Api.UserSocial;

[TestClass]
[TestCategory(TestCategoryNames.Live)]
[TestCategory(TestCategoryNames.UserSocial)]
public sealed class UserSocialSafetyTest : TestBase
{
    [TestMethod]
    public void SafeTargetPortraitFixture_IsExplicitlyRequired_ForLiveFollowMutations()
    {
        var portrait =
            RequireSafeTargetPortraitFixture(
                nameof(SafeTargetPortraitFixture_IsExplicitlyRequired_ForLiveFollowMutations));

        Console.WriteLine($"safeTargetPortrait={portrait}");
    }

    [TestMethod]
    public void SafeTargetUserIdFixture_IsExplicitlyRequired_ForLiveBlacklistAndFanMutations()
    {
        var userId =
            RequireSafeTargetUserIdFixture(
                nameof(SafeTargetUserIdFixture_IsExplicitlyRequired_ForLiveBlacklistAndFanMutations));

        Console.WriteLine($"safeTargetUserId={userId}");
    }
}
