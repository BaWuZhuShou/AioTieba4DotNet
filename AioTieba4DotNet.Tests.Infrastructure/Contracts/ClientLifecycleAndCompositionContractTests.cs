#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using AioTieba4DotNet.Tests.Infrastructure.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Infrastructure.Contracts;

[TestClass]
[TestCategory(OnlineTestContractCategories.Architecture)]
public sealed class ClientLifecycleAndCompositionContractTests
{
    [TestMethod]
    public void TiebaClientCredentialConstructorSupportsGuestAndAuthenticatedComposition()
    {
        using var guestClient = CreateClientViaCredentialConstructor(null, null);
        AssertRootClientSurface(guestClient.Instance);

        var guestSession = GetClientSession(guestClient);
        Assert.IsFalse(GetRequiredBooleanProperty(guestSession, "IsAuthenticated"));
        Assert.IsNull(GetRequiredFieldValue(guestSession, "_account"));

        var authenticatedBduss = CreateValidBduss('b');
        var authenticatedStoken = CreateValidStoken('s');
        using var authenticatedClient = CreateClientViaCredentialConstructor(authenticatedBduss, authenticatedStoken);
        AssertRootClientSurface(authenticatedClient.Instance);

        var authenticatedAccount = GetRequiredFieldValue(GetClientSession(authenticatedClient), "_account");
        Assert.IsNotNull(authenticatedAccount, "Expected the BDUSS constructor overload to attach an authenticated account runtime.");
        Assert.AreEqual(authenticatedBduss, GetRequiredStringProperty(authenticatedAccount, "Bduss"));
        Assert.AreEqual(authenticatedStoken, GetRequiredStringProperty(authenticatedAccount, "Stoken"));
    }

    [TestMethod]
    public void TiebaClientAccountAndOptionsConstructorsMapPublicInputsToRuntimeOptions()
    {
        var accountBduss = CreateValidBduss('a');
        var accountStoken = CreateValidStoken('t');
        var account = CreateAccount(accountBduss, accountStoken);
        using var accountClient = CreateClientViaAccountConstructor(account);
        var accountSession = GetClientSession(accountClient);
        var accountSessionOptions = GetRequiredPropertyValue(accountSession, "Options");

        Assert.AreEqual(accountBduss, GetRequiredStringProperty(accountSessionOptions, "Bduss"));
        Assert.AreEqual(accountStoken, GetRequiredStringProperty(accountSessionOptions, "Stoken"));

        var options = CreateOptions(maxReadRetryAttempts: 3,
            requestTimeout: TimeSpan.FromSeconds(42));
        using var optionsClient = CreateClientViaOptionsConstructor(options);
        AssertRootClientSurface(optionsClient.Instance);

        var optionsSession = GetClientSession(optionsClient);
        var runtimeOptions = GetRequiredPropertyValue(optionsSession, "Options");
        Assert.AreSame(options, runtimeOptions,
            "Expected the public TiebaOptions constructor overload to preserve the caller-supplied options instance inside the runtime session.");
        Assert.AreEqual(3, GetRequiredPropertyValue(runtimeOptions, "MaxReadRetryAttempts"));
        Assert.AreEqual(TimeSpan.FromSeconds(42), GetRequiredPropertyValue(runtimeOptions, "RequestTimeout"));
    }

    [TestMethod]
    public async Task TiebaClientDisposeDisposesDirectConstructionHttpLifetime()
    {
        var client = CreateClientViaOptionsConstructor(CreateOptions(maxReadRetryAttempts: 0));
        var session = GetClientSession(client);
        var httpCore = GetRequiredPropertyValue(session, "HttpCore");
        var httpClient = (HttpClient)GetRequiredPropertyValue(httpCore, "HttpClient");

        client.Dispose();

        await Assert.ThrowsExactlyAsync<ObjectDisposedException>(
            async () => await httpClient.GetAsync("http://localhost"),
            "Expected TiebaClient.Dispose() to dispose the direct-construction HTTP lifetime it owns.");
    }

    [TestMethod]
    public void TiebaClientFactoryPublicConstructorCreatesReusableConcreteFactory()
    {
        using var provider = CreateServiceProvider();
        var factoryType = GetRequiredType(LibraryAssembly, "AioTieba4DotNet.TiebaClientFactory");
        var httpClientFactoryParameterType = factoryType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .Single(static constructor => constructor.GetParameters().Length == 1)
            .GetParameters()[0]
            .ParameterType;
        var httpClientFactory = GetRequiredService(provider.Instance, httpClientFactoryParameterType);
        var factory = Activator.CreateInstance(factoryType, httpClientFactory);

        Assert.IsNotNull(factory, "Expected the public TiebaClientFactory constructor to accept the resolved IHttpClientFactory dependency.");
        Assert.IsTrue(GetRequiredType(LibraryAssembly, "AioTieba4DotNet.ITiebaClientFactory").IsInstanceOfType(factory),
            "Expected the concrete TiebaClientFactory to implement the public ITiebaClientFactory surface.");
    }

    [TestMethod]
    public void TiebaClientFactoryOptionsOverloadsCreateClientsThroughConcreteAndInterfacePaths()
    {
        using var provider = CreateServiceProvider();
        var concreteFactory = CreateConcreteFactory(provider.Instance);
        var interfaceFactory = GetRequiredService(provider.Instance, GetRequiredType(LibraryAssembly, "AioTieba4DotNet.ITiebaClientFactory"));
        var optionsType = GetRequiredType(LibraryAssembly, "AioTieba4DotNet.Contracts.TiebaOptions");

        var concreteOptions = CreateOptions(maxReadRetryAttempts: 4,
            requestTimeout: TimeSpan.FromSeconds(24));
        using var concreteClient = CreateClientViaFactoryOverload(concreteFactory, optionsType, concreteOptions);
        AssertRuntimeOptionsReference(concreteClient, concreteOptions);

        var interfaceOptions = CreateOptions(maxReadRetryAttempts: 5,
            requestTimeout: TimeSpan.FromSeconds(25));
        using var interfaceClient = CreateClientViaFactoryOverload(interfaceFactory, optionsType, interfaceOptions);
        AssertRuntimeOptionsReference(interfaceClient, interfaceOptions);
    }

    [TestMethod]
    public void TiebaClientFactoryCredentialAndAccountOverloadsValidateAndDelegateThroughConcreteAndInterfacePaths()
    {
        using var provider = CreateServiceProvider();
        var concreteFactory = CreateConcreteFactory(provider.Instance);
        var interfaceFactory = GetRequiredService(provider.Instance, GetRequiredType(LibraryAssembly, "AioTieba4DotNet.ITiebaClientFactory"));
        var accountType = GetRequiredType(LibraryAssembly, "AioTieba4DotNet.Contracts.Account");
        var configurationExceptionType = GetRequiredType(LibraryAssembly, "AioTieba4DotNet.TiebaConfigurationException");

        var invalidBdussException = Assert.ThrowsExactly<TargetInvocationException>(
            () => InvokeCreateClient(concreteFactory, typeof(string), " \t", null),
            "Expected the concrete BDUSS overload to reject blank credential input before delegation.");
        Assert.IsInstanceOfType(invalidBdussException.InnerException, configurationExceptionType);
        Assert.AreEqual("The bduss overload requires a non-empty BDUSS value.", invalidBdussException.InnerException!.Message);

        var concreteCredentialBduss = CreateValidBduss('c');
        var concreteCredentialStoken = CreateValidStoken('u');
        using var concreteCredentialClient = InvokeCreateClient(concreteFactory, typeof(string), concreteCredentialBduss, concreteCredentialStoken);
        AssertRuntimeCredentials(concreteCredentialClient, concreteCredentialBduss, concreteCredentialStoken);

        var interfaceCredentialBduss = CreateValidBduss('d');
        var interfaceCredentialStoken = CreateValidStoken('v');
        using var interfaceCredentialClient = InvokeCreateClient(interfaceFactory, typeof(string), interfaceCredentialBduss, interfaceCredentialStoken);
        AssertRuntimeCredentials(interfaceCredentialClient, interfaceCredentialBduss, interfaceCredentialStoken);

        var concreteAccountBduss = CreateValidBduss('e');
        var concreteAccountStoken = CreateValidStoken('w');
        var concreteAccount = Activator.CreateInstance(accountType, concreteAccountBduss, concreteAccountStoken);
        Assert.IsNotNull(concreteAccount, "Expected to create the public Account object through its public constructor.");
        using var concreteAccountClient = CreateClientViaFactoryOverload(concreteFactory, accountType, concreteAccount);
        AssertRuntimeCredentials(concreteAccountClient, concreteAccountBduss, concreteAccountStoken);

        var interfaceAccountBduss = CreateValidBduss('f');
        var interfaceAccountStoken = CreateValidStoken('x');
        var interfaceAccount = Activator.CreateInstance(accountType, interfaceAccountBduss, interfaceAccountStoken);
        Assert.IsNotNull(interfaceAccount, "Expected to create the public Account object through its public constructor.");
        using var interfaceAccountClient = CreateClientViaFactoryOverload(interfaceFactory, accountType, interfaceAccount);
        AssertRuntimeCredentials(interfaceAccountClient, interfaceAccountBduss, interfaceAccountStoken);
    }

    [TestMethod]
    public void AddAioTiebaClientRegistersNamedHttpClientAndExpectedServiceLifetimes()
    {
        using var provider = CreateServiceProvider();
        var rootFactoryFirst = GetRequiredService(provider.Instance,
            GetRequiredType(LibraryAssembly, "AioTieba4DotNet.ITiebaClientFactory"));
        var rootFactorySecond = GetRequiredService(provider.Instance,
            GetRequiredType(LibraryAssembly, "AioTieba4DotNet.ITiebaClientFactory"));

        Assert.AreSame(rootFactoryFirst, rootFactorySecond,
            "Expected AddAioTiebaClient() to register ITiebaClientFactory as a singleton.");
        Assert.AreEqual("AioTieba4DotNet.TiebaClientFactory", rootFactoryFirst.GetType().FullName);

        var httpClientFactory = ResolveNamedHttpClientFactory(provider.Instance);
        using var namedHttpClient = httpClientFactory.CreateClient("TiebaClient");
        Assert.AreEqual(Timeout.InfiniteTimeSpan, namedHttpClient.Timeout,
            "Expected the named TiebaClient HttpClient registration to set Timeout = InfiniteTimeSpan.");

        var primaryHandler = ResolveNamedPrimaryHandler(provider.Instance, "TiebaClient");
        using var handlerLease = CreateDisposableLease(primaryHandler);
        var terminalHandler = UnwrapPrimaryHandler(primaryHandler);
        Assert.IsInstanceOfType(terminalHandler, typeof(HttpClientHandler));
        var httpClientHandler = (HttpClientHandler)terminalHandler;
        Assert.IsTrue(httpClientHandler.UseCookies);
        Assert.IsNotNull(httpClientHandler.CookieContainer);
        Assert.AreEqual(DecompressionMethods.GZip, httpClientHandler.AutomaticDecompression);

        using var firstScope = CreateScope(provider.Instance);
        using var secondScope = CreateScope(provider.Instance);
        var firstScopedClient = GetRequiredService(firstScope.ServiceProvider,
            GetRequiredType(LibraryAssembly, "AioTieba4DotNet.ITiebaClient"));
        var firstScopedClientAgain = GetRequiredService(firstScope.ServiceProvider,
            GetRequiredType(LibraryAssembly, "AioTieba4DotNet.ITiebaClient"));
        var secondScopedClient = GetRequiredService(secondScope.ServiceProvider,
            GetRequiredType(LibraryAssembly, "AioTieba4DotNet.ITiebaClient"));

        Assert.AreSame(firstScopedClient, firstScopedClientAgain,
            "Expected ITiebaClient to be reused within the same DI scope.");
        Assert.AreNotSame(firstScopedClient, secondScopedClient,
            "Expected ITiebaClient to be registered as scoped so different scopes create different clients.");
    }

    [TestMethod]
    public void AddAioTiebaClientInvalidOptionsThrowsTiebaConfigurationExceptionDuringClientResolution()
    {
        using var provider = CreateServiceProvider(options => SetProperty(options, "Stoken", "stoken-without-bduss"));
        using var scope = CreateScope(provider.Instance);
        var clientType = GetRequiredType(LibraryAssembly, "AioTieba4DotNet.ITiebaClient");

        Exception? exception = null;
        try
        {
            _ = GetRequiredService(scope.ServiceProvider, clientType);
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        Assert.IsNotNull(exception,
            "Expected AddAioTiebaClient() to surface TiebaOptions validation errors as TiebaConfigurationException when resolving the scoped ITiebaClient.");
        Assert.AreEqual("AioTieba4DotNet.TiebaConfigurationException", exception.GetType().FullName);
        Assert.AreEqual("Stoken cannot be supplied without Bduss.", exception.Message);
    }

    private static Assembly LibraryAssembly => _libraryAssembly ??= LoadLibraryAssembly();

    private static Assembly? _libraryAssembly;

    private static DisposableLease CreateClientViaCredentialConstructor(string? bduss, string? stoken)
    {
        var clientType = GetRequiredType(LibraryAssembly, "AioTieba4DotNet.TiebaClient");
        var client = clientType.GetConstructor([typeof(string), typeof(string)])?.Invoke([bduss, stoken]);
        Assert.IsNotNull(client, "Expected to create the public TiebaClient through its credential constructor.");
        return CreateDisposableLease(client);
    }

    private static DisposableLease CreateClientViaAccountConstructor(object account)
    {
        var accountType = GetRequiredType(LibraryAssembly, "AioTieba4DotNet.Contracts.Account");
        var client = GetRequiredType(LibraryAssembly, "AioTieba4DotNet.TiebaClient")
            .GetConstructor([accountType])
            ?.Invoke([account]);
        Assert.IsNotNull(client, "Expected to create the public TiebaClient through its account constructor.");
        return CreateDisposableLease(client);
    }

    private static DisposableLease CreateClientViaOptionsConstructor(object options)
    {
        var optionsType = GetRequiredType(LibraryAssembly, "AioTieba4DotNet.Contracts.TiebaOptions");
        var client = GetRequiredType(LibraryAssembly, "AioTieba4DotNet.TiebaClient")
            .GetConstructor([optionsType])
            ?.Invoke([options]);
        Assert.IsNotNull(client, "Expected to create the public TiebaClient through its TiebaOptions constructor.");
        return CreateDisposableLease(client);
    }

    private static object CreateConcreteFactory(object provider)
    {
        var factoryType = GetRequiredType(LibraryAssembly, "AioTieba4DotNet.TiebaClientFactory");
        var constructor = factoryType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .Single(static candidate => candidate.GetParameters().Length == 1);
        var httpClientFactory = GetRequiredService(provider, constructor.GetParameters()[0].ParameterType);
        var factory = constructor.Invoke([httpClientFactory]);
        Assert.IsNotNull(factory, "Expected to create the public TiebaClientFactory from the resolved IHttpClientFactory.");
        return factory;
    }

    private static DisposableLease CreateClientViaFactoryOverload(object factory, Type parameterType, object argument)
    {
        var client = factory.GetType()
            .GetMethod("CreateClient", [parameterType])
            ?.Invoke(factory, [argument]);
        Assert.IsNotNull(client,
            $"Expected '{factory.GetType().FullName}.CreateClient({parameterType.Name})' to return a public client instance.");
        AssertRootClientSurface(client);
        return CreateDisposableLease(client);
    }

    private static DisposableLease InvokeCreateClient(object factory, Type firstParameterType, object? firstArgument,
        object? secondArgument)
    {
        var client = factory.GetType()
            .GetMethod("CreateClient", [firstParameterType, typeof(string)])
            ?.Invoke(factory, [firstArgument, secondArgument]);
        Assert.IsNotNull(client,
            $"Expected '{factory.GetType().FullName}.CreateClient({firstParameterType.Name}, string)' to return a public client instance.");
        AssertRootClientSurface(client);
        return CreateDisposableLease(client);
    }

    private static object CreateAccount(string bduss, string stoken)
    {
        var account = Activator.CreateInstance(GetRequiredType(LibraryAssembly, "AioTieba4DotNet.Contracts.Account"), bduss,
            stoken);
        Assert.IsNotNull(account, "Expected the public Account type to be constructible for factory and root-constructor tests.");
        return account;
    }

    private static object CreateOptions(string? bduss = null, string? stoken = null, int? maxReadRetryAttempts = null,
        TimeSpan? requestTimeout = null)
    {
        var options = Activator.CreateInstance(GetRequiredType(LibraryAssembly, "AioTieba4DotNet.Contracts.TiebaOptions"));
        Assert.IsNotNull(options, "Expected the public TiebaOptions type to be constructible for root and factory tests.");
        SetProperty(options, "Bduss", bduss);
        SetProperty(options, "Stoken", stoken);

        if (maxReadRetryAttempts.HasValue)
            SetProperty(options, "MaxReadRetryAttempts", maxReadRetryAttempts.Value);

        if (requestTimeout.HasValue)
            SetProperty(options, "RequestTimeout", requestTimeout.Value);

        return options;
    }

    private static string CreateValidBduss(char fill)
    {
        return new string(fill, 192);
    }

    private static string CreateValidStoken(char fill)
    {
        return new string(fill, 64);
    }

    private static void AssertRootClientSurface(object client)
    {
        var clientInterface = GetRequiredType(LibraryAssembly, "AioTieba4DotNet.ITiebaClient");
        Assert.IsTrue(clientInterface.IsInstanceOfType(client),
            $"Expected '{client.GetType().FullName}' to implement the public ITiebaClient surface.");

        foreach (var (propertyName, expectedTypeName) in RootSurfaceContracts)
        {
            var property = clientInterface.GetProperty(propertyName);
            Assert.IsNotNull(property, $"Expected ITiebaClient to expose '{propertyName}'.");

            var value = property.GetValue(client);
            Assert.IsNotNull(value, $"Expected ITiebaClient.{propertyName} to return a module instance.");
            Assert.IsTrue(GetRequiredType(LibraryAssembly, expectedTypeName).IsInstanceOfType(value),
                $"Expected ITiebaClient.{propertyName} to implement '{expectedTypeName}'.");
        }
    }

    private static object GetClientSession(DisposableLease client)
    {
        var session = GetRequiredFieldValue(client.Instance, "_lifetime");
        Assert.IsNotNull(session, "Expected TiebaClient to retain a non-null runtime lifetime field.");
        return session;
    }

    private static void AssertRuntimeOptionsReference(DisposableLease client, object expectedOptions)
    {
        var runtimeOptions = GetRequiredPropertyValue(GetClientSession(client), "Options");
        Assert.AreSame(expectedOptions, runtimeOptions,
            "Expected the factory overload to delegate the caller-supplied TiebaOptions instance into the runtime session unchanged.");
    }

    private static void AssertRuntimeCredentials(DisposableLease client, string expectedBduss, string expectedStoken)
    {
        var runtimeOptions = GetRequiredPropertyValue(GetClientSession(client), "Options");
        Assert.AreEqual(expectedBduss, GetRequiredStringProperty(runtimeOptions, "Bduss"));
        Assert.AreEqual(expectedStoken, GetRequiredStringProperty(runtimeOptions, "Stoken"));
    }

    private static ReflectionServiceProviderHandle CreateServiceProvider(Action<object>? configureOptions = null)
    {
        LoadSupportAssembly("Microsoft.Extensions.DependencyInjection.Abstractions");
        LoadSupportAssembly("Microsoft.Extensions.DependencyInjection");
        LoadSupportAssembly("Microsoft.Extensions.Http");
        LoadSupportAssembly("Microsoft.Extensions.Options");

        var services = Activator.CreateInstance(GetRequiredTypeFromLoadedAssemblies(
            "Microsoft.Extensions.DependencyInjection.ServiceCollection"));
        Assert.IsNotNull(services, "Expected the DI test harness to create a ServiceCollection instance.");

        var extensionType = GetRequiredType(LibraryAssembly, "AioTieba4DotNet.DependencyInjection");
        var addAioTiebaClient = extensionType.GetMethod("AddAioTiebaClient", BindingFlags.Public | BindingFlags.Static);
        Assert.IsNotNull(addAioTiebaClient, "Expected the library to expose the public AddAioTiebaClient() extension method.");

        object? configureDelegate = null;
        if (configureOptions is not null)
            configureDelegate = CreateConfigureOptionsDelegate(GetRequiredType(LibraryAssembly, "AioTieba4DotNet.Contracts.TiebaOptions"),
                configureOptions);

        _ = addAioTiebaClient.Invoke(null, [services, configureDelegate]);

        var builderExtensions = GetRequiredTypeFromLoadedAssemblies(
            "Microsoft.Extensions.DependencyInjection.ServiceCollectionContainerBuilderExtensions");
        var buildServiceProvider = builderExtensions.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(static method => method.Name == "BuildServiceProvider" && method.GetParameters().Length == 1);
        var provider = buildServiceProvider.Invoke(null, [services]);
        Assert.IsNotNull(provider, "Expected the DI test harness to build a service provider for AddAioTiebaClient().");
        return new ReflectionServiceProviderHandle(provider);
    }

    private static HttpClientFactoryProxy ResolveNamedHttpClientFactory(object provider)
    {
        var serviceProvider = provider as IServiceProvider;
        Assert.IsNotNull(serviceProvider, "Expected the DI root provider to implement IServiceProvider.");

        var httpClientFactoryType = FindRequiredType("System.Net.Http.IHttpClientFactory");
        var factory = serviceProvider.GetService(httpClientFactoryType);
        Assert.IsNotNull(factory, "Expected AddAioTiebaClient() to register IHttpClientFactory.");
        return new HttpClientFactoryProxy(factory, httpClientFactoryType);
    }

    private static HttpMessageHandler ResolveNamedPrimaryHandler(object provider, string name)
    {
        var messageHandlerFactoryType = FindRequiredType("System.Net.Http.IHttpMessageHandlerFactory");
        var factory = GetRequiredService(provider, messageHandlerFactoryType);
        var handler = messageHandlerFactoryType.GetMethod("CreateHandler")?.Invoke(factory, [name]);
        Assert.IsNotNull(handler, $"Expected the named HttpClient registration '{name}' to expose a primary handler pipeline.");
        return (HttpMessageHandler)handler;
    }

    private static HttpMessageHandler UnwrapPrimaryHandler(HttpMessageHandler handler)
    {
        var current = handler;
        while (current is DelegatingHandler delegatingHandler && delegatingHandler.InnerHandler is not null)
            current = delegatingHandler.InnerHandler;

        return current;
    }

    private static ReflectionServiceScopeHandle CreateScope(object provider)
    {
        var scopeFactoryType = GetRequiredTypeFromLoadedAssemblies("Microsoft.Extensions.DependencyInjection.IServiceScopeFactory");
        var scopeFactory = GetRequiredService(provider, scopeFactoryType);
        var scope = scopeFactoryType.GetMethod("CreateScope")?.Invoke(scopeFactory, []);
        Assert.IsNotNull(scope, "Expected the DI root provider to create scopes for the scoped ITiebaClient registration.");
        return new ReflectionServiceScopeHandle(scope);
    }

    private static object GetRequiredService(object provider, Type serviceType)
    {
        var serviceProvider = provider as IServiceProvider;
        Assert.IsNotNull(serviceProvider, $"Expected '{provider.GetType().FullName}' to implement IServiceProvider.");
        var service = serviceProvider.GetService(serviceType);
        Assert.IsNotNull(service, $"Expected IServiceProvider to resolve '{serviceType.FullName}'.");
        return service!;
    }

    private static object CreateConfigureOptionsDelegate(Type optionsType, Action<object> configureOptions)
    {
        var parameter = Expression.Parameter(optionsType, "options");
        var configureAction = Expression.Constant(configureOptions);
        var invoke = Expression.Call(configureAction, typeof(Action<object>).GetMethod(nameof(Action<object>.Invoke))!,
            Expression.Convert(parameter, typeof(object)));
        var delegateType = typeof(Action<>).MakeGenericType(optionsType);
        return Expression.Lambda(delegateType, invoke, parameter).Compile();
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

    private static Assembly LoadSupportAssembly(string assemblyName)
    {
        return AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(assembly =>
                   string.Equals(assembly.GetName().Name, assemblyName, StringComparison.Ordinal))
               ?? Assembly.LoadFrom(Path.Combine(Path.GetDirectoryName(LibraryAssembly.Location)!, $"{assemblyName}.dll"));
    }

    private static Type FindRequiredType(string fullName)
    {
        var type = AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType(fullName, throwOnError: false))
            .FirstOrDefault(static candidate => candidate is not null);
        Assert.IsNotNull(type, $"Expected to resolve runtime type '{fullName}' from the loaded test or library assemblies.");
        return type!;
    }

    private static Type GetRequiredTypeFromLoadedAssemblies(string fullName)
    {
        return FindRequiredType(fullName);
    }

    private static Type GetRequiredType(Assembly assembly, string fullName)
    {
        var type = assembly.GetType(fullName, throwOnError: false);
        Assert.IsNotNull(type, $"Expected to load type '{fullName}' from '{assembly.Location}'.");
        return type;
    }

    private static object GetRequiredPropertyValue(object instance, string propertyName)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(property, $"Expected type '{instance.GetType().FullName}' to expose property '{propertyName}'.");
        var value = property.GetValue(instance);
        Assert.IsNotNull(value, $"Expected property '{propertyName}' on '{instance.GetType().FullName}' to return a value.");
        return value;
    }

    private static object? GetRequiredFieldValue(object instance, string fieldName)
    {
        var field = instance.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(field, $"Expected type '{instance.GetType().FullName}' to expose field '{fieldName}'.");
        return field.GetValue(instance);
    }

    private static string GetRequiredStringProperty(object instance, string propertyName)
    {
        var value = GetRequiredPropertyValue(instance, propertyName) as string;
        Assert.IsNotNull(value, $"Expected property '{propertyName}' on '{instance.GetType().FullName}' to return a string value.");
        return value;
    }

    private static bool GetRequiredBooleanProperty(object instance, string propertyName)
    {
        return (bool)GetRequiredPropertyValue(instance, propertyName);
    }

    private static void SetProperty(object instance, string propertyName, object? value)
    {
        var property = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(property, $"Expected type '{instance.GetType().FullName}' to expose writable property '{propertyName}'.");
        property.SetValue(instance, value);
    }

    private static DisposableLease CreateDisposableLease(object? instance)
    {
        Assert.IsNotNull(instance, "Expected the reflection harness to create a non-null runtime instance.");
        return new DisposableLease(instance);
    }

    private static readonly (string PropertyName, string ExpectedTypeName)[] RootSurfaceContracts =
    [
        ("Forums", "AioTieba4DotNet.Contracts.IForumModule"),
        ("Threads", "AioTieba4DotNet.Contracts.IThreadModule"),
        ("Users", "AioTieba4DotNet.Contracts.IUserModule"),
        ("Admins", "AioTieba4DotNet.Contracts.IAdminModule"),
        ("Messages", "AioTieba4DotNet.Contracts.IMessagesModule"),
        ("Client", "AioTieba4DotNet.Contracts.IClientModule")
    ];

    private sealed class DisposableLease : IDisposable
    {
        public DisposableLease(object instance)
        {
            Instance = instance;
        }

        public object Instance { get; }

        public void Dispose()
        {
            if (Instance is IDisposable disposable)
                disposable.Dispose();
        }
    }

    private sealed class ReflectionServiceProviderHandle(object instance) : IDisposable
    {
        public object Instance { get; } = instance;

        public void Dispose()
        {
            if (Instance is IDisposable disposable)
                disposable.Dispose();
        }
    }

    private sealed class ReflectionServiceScopeHandle(object instance) : IDisposable
    {
        public object ServiceProvider { get; } = GetRequiredPropertyValue(instance, "ServiceProvider");

        public void Dispose()
        {
            if (instance is IDisposable disposable)
                disposable.Dispose();
        }
    }

    private sealed class HttpClientFactoryProxy(object instance, Type interfaceType)
    {
        public HttpClient CreateClient(string name)
        {
            var client = interfaceType.GetMethod("CreateClient")?.Invoke(instance, [name]);
            Assert.IsNotNull(client, $"Expected IHttpClientFactory.CreateClient('{name}') to return an HttpClient instance.");
            return (HttpClient)client;
        }
    }
}
