#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using AioTieba4DotNet.Tests.Infrastructure.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Infrastructure.Contracts;

[TestClass]
[TestCategory(OnlineTestContractCategories.Architecture)]
public sealed class UserAndMessageSurfaceContractTests
{
    [TestMethod]
    public void UserContentCmdUsersModuleExposesCanonicalPublicConstant()
    {
        using var client = CreateClient();
        var libraryAssembly = client.GetType().Assembly;
        var users = GetRequiredProperty(client, "Users");
        var userModuleInterface = GetRequiredType(libraryAssembly, "AioTieba4DotNet.Contracts.IUserModule");
        var userContentType = GetRequiredType(libraryAssembly, "AioTieba4DotNet.Models.Users.UserContent");
        var expectedCmd = (int)GetRequiredField(userContentType, "Cmd").GetRawConstantValue()!;
        var actualCmd = (int)GetRequiredProperty(userModuleInterface, "UserContentCmd").GetValue(users)!;

        Assert.AreEqual(expectedCmd, actualCmd);
        Assert.AreEqual(303002, actualCmd);
    }

    [TestMethod]
    public void ParsePushNotificationsCapturedPayloadShapeReturnsMappedNotification()
    {
        const long expectedGroupId = 321;
        const long expectedMessageId = 654;
        const int expectedNoteType = 7;
        const string expectedCreateTime = "1712345678";
        const int expectedGroupType = 2;

        using var client = CreateClient();
        var payload = BuildPushNotifyPayload(expectedGroupId, expectedMessageId, expectedNoteType, expectedCreateTime,
            expectedGroupType);

        var messages = GetRequiredProperty(client, "Messages");
        var notifications = ((System.Collections.IEnumerable)GetRequiredMethod(messages.GetType(), "ParsePushNotifications")
                .Invoke(messages, [payload])!)
            .Cast<object>()
            .ToArray();

        Assert.HasCount(1, notifications);
        var notification = notifications[0];
        Assert.AreEqual(expectedGroupId, GetRequiredProperty(notification.GetType(), "GroupId").GetValue(notification));
        Assert.AreEqual(expectedMessageId, GetRequiredProperty(notification.GetType(), "MsgId").GetValue(notification));
        Assert.AreEqual(expectedNoteType, GetRequiredProperty(notification.GetType(), "NoteType").GetValue(notification));
        Assert.AreEqual(long.Parse(expectedCreateTime), GetRequiredProperty(notification.GetType(), "CreateTime").GetValue(notification));
        Assert.AreEqual(expectedGroupType, GetRequiredProperty(notification.GetType(), "GroupType").GetValue(notification));
    }

    private static IDisposable CreateClient()
    {
        var libraryAssembly = LoadLibraryAssembly();
        var clientType = GetRequiredType(libraryAssembly, "AioTieba4DotNet.TiebaClient");
        var client = clientType.GetConstructor([typeof(string), typeof(string)])?.Invoke([null, null]);
        Assert.IsNotNull(client, "Expected to create the public TiebaClient through its credential/guest constructor.");
        return (IDisposable)client;
    }

    private static Assembly LoadLibraryAssembly()
    {
        var candidatePaths = new[]
        {
            Path.Combine(
                RepositoryPaths.GetProjectDirectory("AioTieba4DotNet.Tests.Online.Safe"),
                "bin",
                "Release",
                "net10.0",
                "AioTieba4DotNet.dll"),
            Path.Combine(
                RepositoryPaths.GetProjectDirectory("AioTieba4DotNet"),
                "bin",
                "Release",
                "net10.0",
                "AioTieba4DotNet.dll")
        };

        var libraryPath = candidatePaths.FirstOrDefault(File.Exists);
        if (libraryPath is null)
        {
            Assert.Fail(
                $"Expected a built library assembly at one of: {string.Join(", ", candidatePaths)}. Build the Release safe or library output before running this offline surface contract.");
        }

        return Assembly.LoadFrom(libraryPath);
    }

    private static Type GetRequiredType(Assembly assembly, string fullName)
    {
        var type = assembly.GetType(fullName, throwOnError: false);
        Assert.IsNotNull(type, $"Expected to load type '{fullName}' from '{assembly.Location}'.");
        return type;
    }

    private static PropertyInfo GetRequiredProperty(Type type, string propertyName)
    {
        var property = type.GetProperty(propertyName);
        Assert.IsNotNull(property, $"Expected type '{type.FullName}' to expose property '{propertyName}'.");
        return property;
    }

    private static object GetRequiredProperty(object instance, string propertyName)
    {
        var property = GetRequiredProperty(instance.GetType(), propertyName);
        var value = property.GetValue(instance);
        Assert.IsNotNull(value, $"Expected property '{propertyName}' on '{instance.GetType().FullName}' to return a value.");
        return value;
    }

    private static MethodInfo GetRequiredMethod(Type type, string methodName)
    {
        var method = type.GetMethod(methodName);
        Assert.IsNotNull(method, $"Expected type '{type.FullName}' to expose method '{methodName}'.");
        return method;
    }

    private static FieldInfo GetRequiredField(Type type, string fieldName)
    {
        var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(field, $"Expected type '{type.FullName}' to expose field '{fieldName}'.");
        return field;
    }

    private static byte[] BuildPushNotifyPayload(
        long groupId,
        long messageId,
        int noteType,
        string createTime,
        int groupType)
    {
        using var infoBuffer = new MemoryStream();
        WriteVarintField(infoBuffer, 1, (ulong)groupId);
        WriteVarintField(infoBuffer, 2, (ulong)messageId);
        WriteVarintField(infoBuffer, 4, (ulong)(uint)noteType);
        WriteStringField(infoBuffer, 6, createTime);
        WriteVarintField(infoBuffer, 7, (ulong)(uint)groupType);

        using var pusherBuffer = new MemoryStream();
        WriteLengthDelimitedField(pusherBuffer, 2, infoBuffer.ToArray());

        using var payloadBuffer = new MemoryStream();
        WriteLengthDelimitedField(payloadBuffer, 2, pusherBuffer.ToArray());
        return payloadBuffer.ToArray();
    }

    private static void WriteVarintField(Stream stream, int fieldNumber, ulong value)
    {
        WriteTag(stream, fieldNumber, 0);
        WriteVarint(stream, value);
    }

    private static void WriteStringField(Stream stream, int fieldNumber, string value)
    {
        WriteLengthDelimitedField(stream, fieldNumber, Encoding.UTF8.GetBytes(value));
    }

    private static void WriteLengthDelimitedField(Stream stream, int fieldNumber, byte[] value)
    {
        WriteTag(stream, fieldNumber, 2);
        WriteVarint(stream, (ulong)value.Length);
        stream.Write(value, 0, value.Length);
    }

    private static void WriteTag(Stream stream, int fieldNumber, int wireType)
    {
        WriteVarint(stream, (ulong)((fieldNumber << 3) | wireType));
    }

    private static void WriteVarint(Stream stream, ulong value)
    {
        while (value >= 0x80)
        {
            stream.WriteByte((byte)((value & 0x7Fu) | 0x80u));
            value >>= 7;
        }

        stream.WriteByte((byte)value);
    }
}
