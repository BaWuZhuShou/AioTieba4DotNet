#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using AioTieba4DotNet.Models.Admins;
using AioTieba4DotNet.Contracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Baseline;

[TestClass]
public sealed class ModuleOwnershipContractTests
{
    [TestMethod]
    public void CanonicalOwnerModules_ExposeExpectedRepresentativeFamilies()
    {
        var messagesMethodNames = GetDeclaredMethodNames(typeof(IMessagesModule));
        var adminsMethodNames = GetDeclaredMethodNames(typeof(IAdminModule));
        var forumsMethodNames = GetDeclaredMethodNames(typeof(IForumModule));
        var usersMethodNames = GetDeclaredMethodNames(typeof(IUserModule));

        CollectionAssert.IsSubsetOf(
            new[]
            {
                nameof(IMessagesModule.GetAtsAsync),
                nameof(IMessagesModule.GetRepliesAsync),
                nameof(IMessagesModule.SendMessageAsync),
                nameof(IMessagesModule.SendChatroomMessageAsync),
                nameof(IMessagesModule.SetMessageReadAsync),
                nameof(IMessagesModule.ParsePushNotifications)
            },
            messagesMethodNames.ToArray());

        CollectionAssert.IsSubsetOf(
            new[]
            {
                nameof(IAdminModule.GetBawuInfoAsync),
                nameof(IAdminModule.DelBawuAsync),
                nameof(IAdminModule.BlockAsync),
                nameof(IAdminModule.UnblockAsync)
            },
            adminsMethodNames.ToArray());

        CollectionAssert.IsSubsetOf(
            new[]
            {
                nameof(IForumModule.GetForumAsync),
                nameof(IForumModule.GetFollowForumsAsync),
                nameof(IForumModule.GetSelfFollowForumsAsync),
                nameof(IForumModule.FollowAsync),
                nameof(IForumModule.UnfollowAsync)
            },
            forumsMethodNames.ToArray());

        CollectionAssert.IsSubsetOf(
            new[]
            {
        nameof(IUserModule.GetUserInfoAppAsync),
                nameof(IUserModule.GetProfileAsync),
                nameof(IUserModule.GetHomepageAsync),
                nameof(IUserModule.GetFollowsAsync),
                nameof(IUserModule.GetFansAsync),
        nameof(IUserModule.GetBlacklistAsync),
        nameof(IUserModule.SetBlacklistAsync),
                nameof(IUserModule.SetProfileAsync)
            },
            usersMethodNames.ToArray());
    }

    [TestMethod]
    public void RemovedOldHomeBridges_AreAbsentFromWrongModules_WhileCanonicalOwnersRemain()
    {
        var userMethodNames = GetDeclaredMethodNames(typeof(IUserModule));
        var forumMethodNames = GetDeclaredMethodNames(typeof(IForumModule));

        Assert.DoesNotContain(userMethodNames, nameof(IMessagesModule.GetAtsAsync));
        Assert.DoesNotContain(userMethodNames, nameof(IMessagesModule.GetRepliesAsync));
        Assert.DoesNotContain(userMethodNames, nameof(IAdminModule.BlockAsync));
        Assert.DoesNotContain(forumMethodNames, "DelBaWuAsync");

        Assert.IsFalse(
            HasDeclaredMethod(typeof(IUserModule), nameof(IMessagesModule.GetAtsAsync), typeof(int), typeof(CancellationToken)),
            $"{nameof(IUserModule)} must not reintroduce {nameof(IMessagesModule.GetAtsAsync)} on the wrong module.");
        Assert.IsFalse(
            HasDeclaredMethod(typeof(IUserModule), nameof(IMessagesModule.GetRepliesAsync), typeof(int), typeof(CancellationToken)),
            $"{nameof(IUserModule)} must not reintroduce {nameof(IMessagesModule.GetRepliesAsync)} on the wrong module.");
        Assert.IsFalse(
            HasDeclaredMethod(typeof(IUserModule), nameof(IAdminModule.BlockAsync), typeof(ulong), typeof(string), typeof(int), typeof(string), typeof(CancellationToken)),
            $"{nameof(IUserModule)} must not expose the fid-based block bridge.");
        Assert.IsFalse(
            HasDeclaredMethod(typeof(IUserModule), nameof(IAdminModule.BlockAsync), typeof(string), typeof(string), typeof(int), typeof(string), typeof(CancellationToken)),
            $"{nameof(IUserModule)} must not expose the forum-name block bridge.");
        Assert.IsFalse(
            HasDeclaredMethod(typeof(IForumModule), "DelBaWuAsync", typeof(string), typeof(string), typeof(string), typeof(CancellationToken)),
            $"{nameof(IForumModule)} must not expose the old bawu-removal bridge.");

        Assert.IsTrue(
            HasDeclaredMethod(typeof(IMessagesModule), nameof(IMessagesModule.GetAtsAsync), typeof(int), typeof(CancellationToken)),
            $"{nameof(IMessagesModule)} must remain the canonical owner for {nameof(IMessagesModule.GetAtsAsync)}.");
        Assert.IsTrue(
            HasDeclaredMethod(typeof(IMessagesModule), nameof(IMessagesModule.GetRepliesAsync), typeof(int), typeof(CancellationToken)),
            $"{nameof(IMessagesModule)} must remain the canonical owner for {nameof(IMessagesModule.GetRepliesAsync)}.");
        Assert.IsTrue(
            HasDeclaredMethod(typeof(IAdminModule), nameof(IAdminModule.BlockAsync), typeof(string), typeof(string), typeof(int), typeof(string), typeof(CancellationToken)),
            $"{nameof(IAdminModule)} must remain the canonical owner for block operations.");
        Assert.IsTrue(
            HasDeclaredMethod(typeof(IAdminModule), nameof(IAdminModule.DelBawuAsync), typeof(string), typeof(string), typeof(BawuType), typeof(CancellationToken)),
            $"{nameof(IAdminModule)} must remain the canonical owner for bawu-removal operations.");
    }

    [TestMethod]
    public void ClientModule_RemainsLifecycleOnly()
    {
        var clientMethodNames = GetDeclaredMethodNames(typeof(IClientModule));

        CollectionAssert.AreEquivalent(
            new[]
            {
                nameof(IClientModule.InitWebSocketAsync),
                nameof(IClientModule.InitZIdAsync),
                nameof(IClientModule.SyncAsync)
            },
            clientMethodNames.ToArray());

        Assert.DoesNotContain(clientMethodNames, nameof(IMessagesModule.GetAtsAsync));
        Assert.DoesNotContain(clientMethodNames, nameof(IMessagesModule.GetRepliesAsync));
        Assert.DoesNotContain(clientMethodNames, nameof(IMessagesModule.SendMessageAsync));
        Assert.DoesNotContain(clientMethodNames, nameof(IAdminModule.BlockAsync));
        Assert.DoesNotContain(clientMethodNames, nameof(IForumModule.FollowAsync));
        Assert.DoesNotContain(clientMethodNames, nameof(IUserModule.GetProfileAsync));
    }

    [TestMethod]
    public void TiebaClient_ExposesCanonicalOwnershipModules()
    {
        var properties = typeof(ITiebaClient)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .ToDictionary(property => property.Name, property => property.PropertyType, StringComparer.Ordinal);

        Assert.AreEqual(typeof(IMessagesModule), properties[nameof(ITiebaClient.Messages)]);
        Assert.AreEqual(typeof(IAdminModule), properties[nameof(ITiebaClient.Admins)]);
        Assert.AreEqual(typeof(IClientModule), properties[nameof(ITiebaClient.Client)]);
        Assert.AreEqual(typeof(IForumModule), properties[nameof(ITiebaClient.Forums)]);
        Assert.AreEqual(typeof(IUserModule), properties[nameof(ITiebaClient.Users)]);
    }

    private static IReadOnlyList<string> GetDeclaredMethodNames(Type type) =>
        type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Select(method => method.Name)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

    private static IReadOnlyList<string> GetDeclaredMethodSignatures(Type type, params string[] methodNames) =>
        type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(method => methodNames.Contains(method.Name, StringComparer.Ordinal))
            .Select(FormatMethodSignature)
            .OrderBy(signature => signature, StringComparer.Ordinal)
            .ToArray();

    private static bool HasDeclaredMethod(Type type, string methodName, params Type[] parameterTypes) =>
        type.GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly,
            binder: null,
            types: parameterTypes,
            modifiers: null) is not null;

    private static string FormatMethodSignature(MethodInfo method)
    {
        var parameters = method.GetParameters()
            .Select(parameter => $"{FormatTypeName(parameter.ParameterType)} {parameter.Name}");

        return $"{method.Name}({string.Join(", ", parameters)})";
    }

    private static string FormatTypeName(Type type)
    {
        if (type == typeof(string))
            return nameof(String);

        if (!type.IsGenericType)
            return type.Name;

        var genericTypeName = type.Name[..type.Name.IndexOf('`')];
        var genericArguments = type.GetGenericArguments().Select(FormatTypeName);
        return $"{genericTypeName}<{string.Join(", ", genericArguments)}>";
    }
}
