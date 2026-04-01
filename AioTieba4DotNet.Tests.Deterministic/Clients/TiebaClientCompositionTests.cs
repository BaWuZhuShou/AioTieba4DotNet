#nullable enable
using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Linq;
using System.Threading.Tasks;
using AioTieba4DotNet.Contracts;
using AioTieba4DotNet.Tests.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Clients;

[TestClass]
public sealed class TiebaClientCompositionTests
{
    [TestMethod]
    public void TiebaClient_PublicContract_UsesSixNormalizedModules()
    {
        var moduleProperties = typeof(ITiebaClient)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .OrderBy(property => property.Name, StringComparer.Ordinal)
            .Select(property => property.Name)
            .ToArray();

        CollectionAssert.AreEqual(
            new[] { "Admins", "Client", "Forums", "Messages", "Threads", "Users" },
            moduleProperties);

        var clientSource = RepositorySourceTextAssert.ReadRepositoryFiles(
            "AioTieba4DotNet/Clients/ITiebaClient.cs",
            "AioTieba4DotNet/Clients/TiebaClient.cs");
        RepositorySourceTextAssert.ContainsAll(clientSource, "Admins", "Messages", "Client");
    }

    [TestMethod]
    public async Task DirectFactoryAndDependencyInjectionClients_BehaveEquivalently()
    {
        var services = new ServiceCollection();
        services.AddAioTiebaClient(options => options.TransportMode = TiebaTransportMode.Http);
        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<ITiebaClientFactory>();
        using var directClient = new TiebaClient(CreateGuestOptions());
        using var factoryClient = factory.CreateClient(CreateGuestOptions());
        using var scope = provider.CreateScope();
        var diClient = scope.ServiceProvider.GetRequiredService<ITiebaClient>();

        AssertEquivalentModules(directClient, factoryClient);
        AssertEquivalentModules(directClient, diClient);

        _ = await ThrowsAsync<TiebaAuthenticationException>(() => directClient.Users.FollowAsync("portrait"));
        _ = await ThrowsAsync<TiebaAuthenticationException>(() => factoryClient.Users.FollowAsync("portrait"));
        _ = await ThrowsAsync<TiebaAuthenticationException>(() => diClient.Users.FollowAsync("portrait"));
    }

    [TestMethod]
    public void InvalidOptions_FailEarlyAcrossDirectFactoryAndDependencyInjection()
    {
        var invalidOptions = new TiebaOptions { Stoken = "stoken-only" };
        var services = new ServiceCollection();
        services.AddAioTiebaClient(options => options.Stoken = "stoken-only");
        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<ITiebaClientFactory>();

        var directException = Throws<TiebaConfigurationException>(() => _ = new TiebaClient(invalidOptions));
        var factoryException = Throws<TiebaConfigurationException>(() => _ = factory.CreateClient(invalidOptions));

        using var scope = provider.CreateScope();
        var diException = Throws<TiebaConfigurationException>(
            () => _ = scope.ServiceProvider.GetRequiredService<ITiebaClient>());

        Assert.AreEqual("Stoken cannot be supplied without Bduss.", directException.Message);
        Assert.AreEqual(directException.Message, factoryException.Message);
        Assert.AreEqual(directException.Message, diException.Message);
    }

    [TestMethod]
    public void PublicExceptionFamilies_RemainDistinct()
    {
        Assert.AreEqual(typeof(TiebaException), typeof(TiebaProtocolException).BaseType);
        Assert.AreEqual(typeof(TiebaException), typeof(TiebaUnsupportedOperationException).BaseType);
        Assert.AreNotEqual(typeof(TiebaProtocolException), typeof(TiebaUnsupportedOperationException));
    }

    [TestMethod]
    public void TiebaClientFactory_StringOverload_RejectsWhitespaceBduss()
    {
        var factory = new TiebaClientFactory(TiebaClientComposition.Direct);

        var exception = Throws<TiebaConfigurationException>(() => _ = factory.CreateClient(" \t ", "stoken"));

        Assert.AreEqual("The bduss overload requires a non-empty BDUSS value.", exception.Message);
    }

    [TestMethod]
    public void TiebaClientFactory_StringAndOptionsOverloads_CreateUsableClients()
    {
        var factory = new TiebaClientFactory(TiebaClientComposition.Direct);

        using var credentialClient = factory.CreateClient(new string('b', 192), new string('s', 64));
        using var guestClient = factory.CreateClient(CreateGuestOptions());

        Assert.IsNotNull(credentialClient.Forums);
        Assert.IsNotNull(credentialClient.Threads);
        Assert.IsNotNull(credentialClient.Users);
        Assert.IsNotNull(credentialClient.Admins);
        Assert.IsNotNull(credentialClient.Messages);
        Assert.IsNotNull(credentialClient.Client);
        Assert.IsNotNull(guestClient.Forums);
        Assert.IsNotNull(guestClient.Threads);
        Assert.IsNotNull(guestClient.Users);
        Assert.IsNotNull(guestClient.Admins);
        Assert.IsNotNull(guestClient.Messages);
        Assert.IsNotNull(guestClient.Client);
    }

    [TestMethod]
    public void AccountTimeoutAndVersionExports_AreUsableOnPublicSurface()
    {
        var account = new AioTieba4DotNet.Contracts.Account(new string('b', 192), new string('s', 64));
        var factory = new TiebaClientFactory(TiebaClientComposition.Direct);

        using var accountClient = new TiebaClient(account);
        using var factoryClient = factory.CreateClient(account);

        var options = account.ToTiebaOptions();
        options.Timeout = new TimeoutConfig { RequestTimeout = TimeSpan.FromSeconds(15), MaxReadRetryAttempts = 1 };
        var timeout = options.Timeout;
        var requestTimeout = options.RequestTimeout;
        var maxReadRetryAttempts = options.MaxReadRetryAttempts;
        options.Timeout = null!;

        Assert.IsNotNull(accountClient.Users);
        Assert.IsNotNull(factoryClient.Users);
        Assert.AreEqual(TimeSpan.FromSeconds(15), timeout.RequestTimeout);
        Assert.AreEqual(1, timeout.MaxReadRetryAttempts);
        Assert.AreEqual(TimeSpan.FromSeconds(15), requestTimeout);
        Assert.AreEqual(1, maxReadRetryAttempts);
        Assert.AreEqual(TimeSpan.FromSeconds(30), options.Timeout.RequestTimeout);
        Assert.AreEqual(0, options.Timeout.MaxReadRetryAttempts);
        Assert.IsFalse(string.IsNullOrWhiteSpace(VersionInfo.Version));
    }

    [TestMethod]
    public void TiebaClient_AccountOverload_RejectsNullAccount()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = new TiebaClient((AioTieba4DotNet.Contracts.Account)null!));
    }

    [TestMethod]
    public void TiebaLogging_ProvidesLoggers_AndCanWriteFileLogs()
    {
        var logPath = Path.Combine(Path.GetTempPath(), $"aiotieba4dotnet-{Guid.NewGuid():N}.log");

        try
        {
            var factory = TiebaLogging.EnableFileLog(logPath, LogLevel.Debug);
            var logger = TiebaLogging.GetLogger("ParityTest");
            var filteredFactory = TiebaLogging.EnableFileLog(logPath, LogLevel.Information);
            var filteredLogger = TiebaLogging.GetLogger("ParityQuiet");

            using (logger.BeginScope("scope"))
            {
            }

            logger.LogInformation("safe message");
            logger.LogError(new InvalidOperationException("inner"), "safe error");
            InvokeRawLogger(CreateFileLogger(logPath, LogLevel.Information), LogLevel.Debug, "suppressed debug");
            CreateFileLoggerProvider(logPath, LogLevel.Information).Dispose();

            Assert.AreSame(filteredFactory, TiebaLogging.Factory);
            Assert.IsTrue(File.Exists(logPath));
            var logText = File.ReadAllText(logPath);
            StringAssert.Contains(logText, "safe message");
            StringAssert.Contains(logText, "safe error");
            StringAssert.Contains(logText, "InvalidOperationException: inner");
            Assert.DoesNotContain("suppressed debug", logText);
            Assert.IsNotNull(TiebaLogging.GetLogger<TiebaClientCompositionTests>());
            filteredFactory.Dispose();
        }
        finally
        {
            TiebaLogging.Reset();
            if (File.Exists(logPath))
                File.Delete(logPath);
        }
    }

    [TestMethod]
    public void TiebaClientFactory_PublicConstructor_UsesProvidedHttpClientFactory()
    {
        var httpClientFactory = new RecordingHttpClientFactory();
        var factory = new TiebaClientFactory(httpClientFactory);

        using var client = factory.CreateClient(CreateGuestOptions());

        Assert.AreEqual(1, httpClientFactory.CreateClientCallCount);
        Assert.AreEqual(DependencyInjection.HttpClientName, httpClientFactory.LastName);
        Assert.IsNotNull(client.Client);
    }

    private static TiebaOptions CreateGuestOptions() => new()
    {
        TransportMode = TiebaTransportMode.Http
    };

    private static void AssertEquivalentModules(ITiebaClient expected, ITiebaClient actual)
    {
        Assert.AreEqual(expected.Forums.GetType(), actual.Forums.GetType());
        Assert.AreEqual(expected.Threads.GetType(), actual.Threads.GetType());
        Assert.AreEqual(expected.Users.GetType(), actual.Users.GetType());
        Assert.AreEqual(expected.Admins.GetType(), actual.Admins.GetType());
        Assert.AreEqual(expected.Messages.GetType(), actual.Messages.GetType());
        Assert.AreEqual(expected.Client.GetType(), actual.Client.GetType());
    }

    private static TException Throws<TException>(System.Action action)
        where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException exception)
        {
            return exception;
        }

        Assert.Fail($"Expected {typeof(TException).Name} was not thrown.");
        throw new System.InvalidOperationException();
    }

    private static async Task<TException> ThrowsAsync<TException>(Func<Task> action)
        where TException : Exception
    {
        try
        {
            await action();
        }
        catch (TException exception)
        {
            return exception;
        }

        Assert.Fail($"Expected {typeof(TException).Name} was not thrown.");
        throw new System.InvalidOperationException();
    }

    private static void InvokeRawLogger(ILogger logger, LogLevel logLevel, string state)
    {
        var method = logger.GetType().GetMethod("Log")!.MakeGenericMethod(typeof(string));
        method.Invoke(logger,
        [
            logLevel,
            new EventId(1, "raw"),
            state,
            null,
            (Func<string, Exception?, string>)((message, _) => message)
        ]);
    }

    private static IDisposable CreateFileLoggerProvider(string filePath, LogLevel minimumLevel)
    {
        var providerType = typeof(TiebaLogging).GetNestedType("FileLoggerProvider", BindingFlags.NonPublic)!;
        return (IDisposable)Activator.CreateInstance(providerType, filePath, minimumLevel)!;
    }

    private static ILogger CreateFileLogger(string filePath, LogLevel minimumLevel)
    {
        var loggerType = typeof(TiebaLogging).GetNestedType("FileLogger", BindingFlags.NonPublic)!;
        return (ILogger)Activator.CreateInstance(loggerType, filePath, "ParityQuietRaw", minimumLevel)!;
    }

    private sealed class RecordingHttpClientFactory : IHttpClientFactory
    {
        public int CreateClientCallCount { get; private set; }

        public string? LastName { get; private set; }

        public HttpClient CreateClient(string name)
        {
            CreateClientCallCount++;
            LastName = name;
            return new HttpClient();
        }
    }
}
