#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AioTieba4DotNet.Internal.Mapping;
using AioTieba4DotNet.Models.Shared;
using Google.Protobuf;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace AioTieba4DotNet.Tests.Mapping;

[TestClass]
public sealed class ModelBoundaryMappingTests
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
    public void User_info_mapping_handles_null_and_group_message_shapes()
    {
        Assert.IsNull(UserInfoMapper.FromTbData((PostInfoList?)null));
        Assert.IsNull(UserInfoMapper.FromTbData((User?)null));

        var fromJsonFallback = UserInfoMapper.FromTbData(new JObject());
        var fromUser = UserInfoMapper.FromTbData(new User
        {
            Id = 7, Portrait = "plain-portrait", Name = "proto-name", NameShow = "Proto Show"
        });
        var fromGroupMessage = UserInfoMapper.FromTbData(
            new GetGroupMsgResIdl.Types.DataRes.Types.GroupMsg.Types.MsgInfo.Types.UserInfo
            {
                UserId = 8, UserName = "group-user"
            });

        Assert.AreEqual(0L, fromJsonFallback.UserId);
        Assert.AreEqual(string.Empty, fromJsonFallback.Portrait);
        Assert.AreEqual(string.Empty, fromJsonFallback.UserName);
        Assert.AreEqual(string.Empty, fromJsonFallback.NickNameNew);
        Assert.IsNotNull(fromUser);
        Assert.AreEqual("plain-portrait", fromUser!.Portrait);
        Assert.AreEqual("proto-name", fromUser.UserName);
        Assert.AreEqual("Proto Show", fromUser.NickNameNew);
        Assert.AreEqual(8L, fromGroupMessage.UserId);
        Assert.AreEqual(string.Empty, fromGroupMessage.Portrait);
        Assert.AreEqual("group-user", fromGroupMessage.UserName);

        var jsonMapped = UserInfoJsonMapper.FromTbData(new JObject
        {
            ["id"] = 9, ["portrait"] = "tb.1.json?012345678901", ["name"] = "json-name", ["name_show"] = "Json Show"
        });
        var jsonFallback = UserInfoJsonMapper.FromTbData(new JObject());

        Assert.AreEqual(9L, jsonMapped.UserId);
        Assert.AreEqual("tb.1.json", jsonMapped.Portrait);
        Assert.AreEqual("json-name", jsonMapped.UserName);
        Assert.AreEqual("Json Show", jsonMapped.NickNameNew);
        Assert.AreEqual(0L, jsonFallback.UserId);
        Assert.AreEqual(string.Empty, jsonFallback.Portrait);
    }

    [TestMethod]
    public void User_info_t_mapping_handles_null_and_default_flags()
    {
        Assert.IsNull(UserInfoTMapper.FromTbData(null));

        var mapped = UserInfoTMapper.FromTbData(new User
        {
            Id = 9,
            Name = "thread-user",
            NameShow = "Thread Show",
            Gender = 0,
            PrivSets = new User.Types.PrivSets { Like = 0, Reply = 0 }
        });

        Assert.IsNotNull(mapped);
        Assert.AreEqual(9L, mapped!.UserId);
        Assert.AreEqual(string.Empty, mapped.Portrait);
        Assert.AreEqual("thread-user", mapped.UserName);
        Assert.AreEqual("Thread Show", mapped.NickNameNew);
        Assert.AreEqual(0, mapped.GLevel);
        Assert.AreEqual(default, mapped.Gender);
        Assert.AreEqual(0, mapped.Icons.Count);
        Assert.IsFalse(mapped.IsBawu);
        Assert.IsFalse(mapped.IsVip);
        Assert.IsFalse(mapped.IsGod);
        Assert.AreEqual("Public", mapped.PrivLike.ToString());
        Assert.AreEqual("All", mapped.PrivReply.ToString());
        Assert.AreEqual(string.Empty, mapped.Ip);
    }

    [TestMethod]
    public void User_info_t_mapping_handles_rich_protobuf_shape()
    {
        var mapped = UserInfoTMapper.FromTbData(new User
        {
            Id = 10,
            Portrait = "tb.1.rich?012345678901",
            Name = "rich-user",
            NameShow = "Rich Show",
            LevelId = 5,
            Gender = 1,
            IsBawu = 1,
            IpAddress = "Earth",
            UserGrowth = new User.Types.UserGrowth { LevelId = 7 },
            NewGodData = new User.Types.NewGodInfo { Status = 1 },
            PrivSets = new User.Types.PrivSets { Like = 1, Reply = 2 },
            Iconinfo = { new User.Types.Icon { Name = "icon-a" }, new User.Types.Icon { Name = string.Empty } },
            NewTshowIcon = { new User.Types.TshowInfo { Name = "vip" } }
        });

        Assert.IsNotNull(mapped);
        Assert.AreEqual("tb.1.rich", mapped!.Portrait);
        Assert.AreEqual(5, mapped.Level);
        Assert.AreEqual(7, mapped.GLevel);
        Assert.AreEqual(1, mapped.Icons.Count);
        Assert.AreEqual("icon-a", mapped.Icons[0]);
        Assert.IsTrue(mapped.IsBawu);
        Assert.IsTrue(mapped.IsVip);
        Assert.IsTrue(mapped.IsGod);
        Assert.AreEqual("Public", mapped.PrivLike.ToString());
        Assert.AreEqual("2", mapped.PrivReply.ToString());
        Assert.AreEqual("Earth", mapped.Ip);
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

    [TestMethod]
    public void Public_models_and_contracts_do_not_expose_transport_or_mapping_signatures()
    {
        var assembly = typeof(TiebaOptions).Assembly;

        var publicSurfaceTypes = assembly
            .GetTypes()
            .Where(type => type.IsPublic)
            .Where(type =>
            {
                var namespaceName = type.Namespace;
                return namespaceName is not null
                       && (namespaceName.StartsWith("AioTieba4DotNet.Models", StringComparison.Ordinal)
                           || namespaceName.StartsWith("AioTieba4DotNet.Contracts", StringComparison.Ordinal));
            })
            .ToList();

        Assert.IsNotEmpty(publicSurfaceTypes);

        foreach (var type in publicSurfaceTypes)
        {
            Assert.IsFalse(typeof(IMessage).IsAssignableFrom(type),
                $"{type.FullName} should not be a protobuf message type.");

            var mappingMethods = type
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static |
                            BindingFlags.DeclaredOnly)
                .Where(method => method.Name is "FromTbData" or "FromProto" or "ToProto")
                .Select(method => method.Name)
                .ToList();

            Assert.IsFalse(mappingMethods.Any(),
                $"{type.FullName} exposes mapping helpers: {string.Join(", ", mappingMethods)}");

            var leakedSignatureTypes = GetPublicSignatureTypes(type)
                .SelectMany(ExpandSignatureTypes)
                .Distinct()
                .Where(IsTransportLeak)
                .Select(candidate => candidate.FullName ?? candidate.Name)
                .ToList();

            Assert.IsFalse(leakedSignatureTypes.Any(),
                $"{type.FullName} exposes transport types: {string.Join(", ", leakedSignatureTypes)}");
        }
    }

    private static IEnumerable<Type> GetPublicSignatureTypes(Type type)
    {
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            yield return property.PropertyType;

        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            yield return field.FieldType;

        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static |
                                               BindingFlags.DeclaredOnly))
        {
            if (method.IsSpecialName) continue;

            yield return method.ReturnType;

            foreach (var parameter in method.GetParameters()) yield return parameter.ParameterType;
        }
    }

    private static IEnumerable<Type> ExpandSignatureTypes(Type type)
    {
        if (type.IsByRef) type = type.GetElementType()!;

        yield return type;

        if (type.IsArray)
        {
            foreach (var nestedType in ExpandSignatureTypes(type.GetElementType()!)) yield return nestedType;

            yield break;
        }

        if (!type.IsGenericType) yield break;

        foreach (var argument in type.GetGenericArguments())
        foreach (var nestedType in ExpandSignatureTypes(argument))
            yield return nestedType;
    }

    private static bool IsTransportLeak(Type type)
    {
        var namespaceName = type.Namespace;
        return typeof(IMessage).IsAssignableFrom(type)
               || namespaceName?.StartsWith("AioTieba4DotNet.Internal", StringComparison.Ordinal) == true
               || namespaceName?.StartsWith("AioTieba4DotNet.Api", StringComparison.Ordinal) == true;
    }
}
