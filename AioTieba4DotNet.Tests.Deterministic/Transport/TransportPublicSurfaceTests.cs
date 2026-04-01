#nullable enable
using System.Linq;
using AioTieba4DotNet.Tests.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Transport;

[TestClass]
public class TransportPublicSurfaceTests
{
    [TestMethod]
    public void LowLevelConcreteTransportTypes_AreNotExported()
    {
        var exportedTypeNames = typeof(TiebaClient).Assembly
            .GetExportedTypes()
            .Select(type => type.FullName)
            .ToArray();

        CollectionAssert.DoesNotContain(exportedTypeNames, "AioTieba4DotNet.Transport.Http.HttpCore");
        CollectionAssert.DoesNotContain(exportedTypeNames, "AioTieba4DotNet.Transport.WebSockets.WebsocketCore");
    }

    [TestMethod]
    public void RenamedPublicTypes_AreExportedWithoutLegacyAliases()
    {
        var exportedTypeNames = typeof(TiebaClient).Assembly
            .GetExportedTypes()
            .Select(type => type.FullName)
            .ToArray();

        CollectionAssert.Contains(exportedTypeNames, "AioTieba4DotNet.Models.Users.UserPostGroups");
        CollectionAssert.Contains(exportedTypeNames, "AioTieba4DotNet.Models.Users.BlacklistUsers");
        CollectionAssert.Contains(exportedTypeNames, "AioTieba4DotNet.Models.Users.BlacklistOldUsers");
        CollectionAssert.Contains(exportedTypeNames, "AioTieba4DotNet.Models.Users.UserInfoGuInfoApp");
        CollectionAssert.Contains(exportedTypeNames, "AioTieba4DotNet.Models.Users.UserInfoGuInfoWeb");
        CollectionAssert.Contains(exportedTypeNames, "AioTieba4DotNet.Models.Forums.SelfFollowForumsV1");
        CollectionAssert.Contains(exportedTypeNames, "AioTieba4DotNet.Models.Forums.SelfFollowForumV1");
        CollectionAssert.DoesNotContain(exportedTypeNames, "AioTieba4DotNet.Models.Users.UserPostss");
        CollectionAssert.DoesNotContain(exportedTypeNames, "AioTieba4DotNet.Models.Users.BlacklistPermissions");
        CollectionAssert.DoesNotContain(exportedTypeNames, "AioTieba4DotNet.Models.Users.BlacklistMutedUsers");
        CollectionAssert.DoesNotContain(exportedTypeNames, "AioTieba4DotNet.Models.Users.UserInfoApp");
        CollectionAssert.DoesNotContain(exportedTypeNames, "AioTieba4DotNet.Models.Users.UserInfoWeb");
        CollectionAssert.DoesNotContain(exportedTypeNames, "AioTieba4DotNet.Models.Forums.SelfFollowForumsPaged");
        CollectionAssert.DoesNotContain(exportedTypeNames, "AioTieba4DotNet.Models.Forums.SelfFollowForumV1Item");

        var clientSource = RepositorySourceTextAssert.ReadRepositoryFiles("AioTieba4DotNet/Clients/ITiebaClient.cs");
        RepositorySourceTextAssert.ContainsAll(clientSource, "IAdminModule Admins", "IMessagesModule Messages",
            "IClientModule Client");
    }
}
