using System;
using System.Linq;
using System.Reflection;
using AioTieba4DotNet.Protocols;
using AioTieba4DotNet.Tests.Platform.Contracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Governance.Contracts;

[TestClass]
[TestCategory(OnlineTestContractCategories.Architecture)]
public sealed class ForumLookupArchitectureContractTests
{
    [TestMethod]
    public void ThreadAndUserProtocolsDependOnNarrowForumQueries()
    {
        AssertNarrowForumDependencies(typeof(ThreadProtocol),
            typeof(IForumIdentityResolver), typeof(IForumCategoryResolver));
        AssertNarrowForumDependencies(typeof(UserProtocol), typeof(IForumIdentityResolver));
    }

    private static void AssertNarrowForumDependencies(Type protocolType, params Type[] queryTypes)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        var constructorTypes = protocolType.GetConstructors(flags)
            .SelectMany(constructor => constructor.GetParameters())
            .Select(parameter => parameter.ParameterType)
            .ToArray();
        var fieldTypes = protocolType.GetFields(flags).Select(field => field.FieldType).ToArray();

        foreach (var dependencyType in constructorTypes.Concat(fieldTypes))
        {
            Assert.IsFalse(typeof(IForumProtocol).IsAssignableFrom(dependencyType),
                $"{protocolType.Name} 只能通过窄查询接口使用论坛能力，不能依赖 {dependencyType.Name}。");
            Assert.AreNotEqual(typeof(ForumIdentityResolver), dependencyType,
                $"{protocolType.Name} 应依赖身份查询接口，不能获得具体服务的缓存与管理操作。");
        }

        foreach (var queryType in queryTypes)
        {
            Assert.IsTrue(constructorTypes.Contains(queryType),
                $"{protocolType.Name} 必须通过构造参数接收 {queryType.Name}。");
            Assert.IsTrue(fieldTypes.Contains(queryType),
                $"{protocolType.Name} 必须保留 {queryType.Name} 作为查询依赖。");
        }
    }
}
