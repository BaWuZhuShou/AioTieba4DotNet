using System.Linq;
using System.Reflection;
using AioTieba4DotNet.Internal.Mapping;
using AioTieba4DotNet.Models.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Tests.Mapping;

[TestClass]
public class ModelBoundaryMappingTests
{
    [TestMethod]
    public void User_info_json_mapping_stays_internal()
    {
        var data = JObject.Parse("""
                                 {
                                   "id": 42,
                                   "portrait": "tb.1.json-user?123456789012",
                                   "name": "json-user",
                                   "name_show": "Json Show"
                                 }
                                 """);

        var mapped = UserInfoMapper.FromTbData(data);

        Assert.AreEqual(42L, mapped.UserId);
        Assert.AreEqual("tb.1.json-user", mapped.Portrait);
        Assert.AreEqual("json-user", mapped.UserName);
        Assert.AreEqual("Json Show", mapped.NickNameNew);
    }

    [TestMethod]
    public void User_info_protobuf_mapping_stays_internal()
    {
        var data = new PostInfoList
        {
            UserId = 123456,
            UserPortrait = "tb.1.proto-user?123456789012",
            UserName = "proto-user",
            NameShow = "Proto Show"
        };

        var mapped = UserInfoMapper.FromTbData(data);

        Assert.IsNotNull(mapped);
        Assert.AreEqual(123456L, mapped!.UserId);
        Assert.AreEqual("tb.1.proto-user", mapped.Portrait);
        Assert.AreEqual("proto-user", mapped.UserName);
        Assert.AreEqual("Proto Show", mapped.NickNameNew);
    }

    [TestMethod]
    public void Public_user_info_contract_is_shape_only()
    {
        var fromTbDataMethods = typeof(UserInfo)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(method => method.Name == "FromTbData")
            .ToList();

        Assert.AreEqual("AioTieba4DotNet.Models.Shared", typeof(UserInfo).Namespace);
        Assert.IsEmpty(fromTbDataMethods);
    }
}
