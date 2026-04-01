using System;
using AioTieba4DotNet.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Api.Admins;

[TestClass]
[TestCategory(TestCategoryNames.Live)]
[TestCategory(TestCategoryNames.ForumExtensions)]
public sealed class AdminMutationSafetyTest : TestBase
{
    [TestMethod]
    public void AdminMutationManualGate_IsExplicitlyRequired_ForLiveTask13Mutations()
    {
        EnsureAdminMutationManualGate(nameof(AdminMutationManualGate_IsExplicitlyRequired_ForLiveTask13Mutations));

        Console.WriteLine($"adminMutationGate={EnableAdminMutationTests}");
    }

    [TestMethod]
    public void SafeTargetUserNameFixture_IsExplicitlyRequired_ForLiveAddBaWuMutations()
    {
        EnsureAdminMutationManualGate(nameof(SafeTargetUserNameFixture_IsExplicitlyRequired_ForLiveAddBaWuMutations));
        var userName = RequireSafeTargetUserNameFixture(nameof(SafeTargetUserNameFixture_IsExplicitlyRequired_ForLiveAddBaWuMutations));

        Console.WriteLine($"safeTargetUserName={userName}");
    }

    [TestMethod]
    public void SafeTargetUserIdFixture_IsExplicitlyRequired_ForLiveBlacklistAndUnblockMutations()
    {
        EnsureAdminMutationManualGate(nameof(SafeTargetUserIdFixture_IsExplicitlyRequired_ForLiveBlacklistAndUnblockMutations));
        var userId = RequireSafeTargetUserIdFixture(nameof(SafeTargetUserIdFixture_IsExplicitlyRequired_ForLiveBlacklistAndUnblockMutations));

        Console.WriteLine($"safeTargetUserId={userId}");
    }

    [TestMethod]
    public void SafeTargetPortraitFixture_IsExplicitlyRequired_ForLiveBaWuRollbackMutations()
    {
        EnsureAdminMutationManualGate(nameof(SafeTargetPortraitFixture_IsExplicitlyRequired_ForLiveBaWuRollbackMutations));
        var portrait = RequireSafeTargetPortraitFixture(nameof(SafeTargetPortraitFixture_IsExplicitlyRequired_ForLiveBaWuRollbackMutations));

        Console.WriteLine($"safeTargetPortrait={portrait}");
    }
}
