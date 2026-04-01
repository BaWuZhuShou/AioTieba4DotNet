#nullable enable
using System.Linq;
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
}
