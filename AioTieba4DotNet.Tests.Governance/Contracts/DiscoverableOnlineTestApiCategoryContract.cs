#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using AioTieba4DotNet.Tests.Platform.Contracts;
using AioTieba4DotNet.Tests.Platform.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Governance.Contracts;

[ExcludeFromCodeCoverage]
public static class DiscoverableOnlineTestApiCategoryContract
{
    private const string TargetFramework = "net10.0";

    public static readonly DiscoverableOnlineTestApiCategoryUsage[] DiscoverableTests = LoadDiscoverableTests();

    public static readonly string[] DiscoverableFirstClassApiCategories = BuildDiscoverableFirstClassApiCategories();

    public static DiscoverableOnlineTestApiCategoryUsage[] GetTestsForCategory(string category)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(category);

        return
        [
            .. DiscoverableTests.Where(test => test.Categories.Contains(category, StringComparer.Ordinal))
        ];
    }

    private static DiscoverableOnlineTestApiCategoryUsage[] LoadDiscoverableTests()
    {
        var discoverableTests = new List<DiscoverableOnlineTestApiCategoryUsage>();

        foreach (var projectName in OnlineTestProjectTopology.ScenarioProjectNames)
        {
            var assembly = Assembly.LoadFrom(ResolveScenarioAssemblyPath(projectName));
            foreach (var testClass in GetLoadableTypes(assembly).Where(IsDiscoverableTestClass))
            {
                var classCategories = GetTestCategories(testClass.GetCustomAttributes(inherit: false));
                foreach (var testMethod in testClass.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                             .Where(IsDiscoverableTestMethod))
                {
                    var categories = ToDistinctArray(
                        classCategories
                            .Concat(GetTestCategories(testMethod.GetCustomAttributes(inherit: false)))
                            .Where(OnlineTestApiCategories.IsWellFormedFirstClassCategory));
                    if (categories.Length == 0)
                        continue;

                    discoverableTests.Add(new DiscoverableOnlineTestApiCategoryUsage(
                        projectName,
                        testClass.FullName ?? testClass.Name,
                        testMethod.Name,
                        categories));
                }
            }
        }

        return [.. discoverableTests];
    }

    private static string[] BuildDiscoverableFirstClassApiCategories()
    {
        return ToDistinctArray(DiscoverableTests.SelectMany(static test => test.Categories));
    }

    private static string ResolveScenarioAssemblyPath(string projectName)
    {
        var projectDirectory = RepositoryPaths.GetProjectDirectory(projectName);
        var candidatePaths = new[]
        {
            Path.Combine(projectDirectory, "bin", "Release", TargetFramework, $"{projectName}.dll"),
            Path.Combine(projectDirectory, "bin", "Debug", TargetFramework, $"{projectName}.dll")
        };

        var assemblyPath = candidatePaths.FirstOrDefault(File.Exists);
        if (assemblyPath is not null)
            return assemblyPath;

        throw new FileNotFoundException(
            $"Expected a built scenario test assembly for '{projectName}' at one of: {string.Join(", ", candidatePaths)}. Build the solution before running the discoverable Api:* guardrail.");
    }

    private static Type[] GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            var loaderMessages = exception.LoaderExceptions
                .Where(static loaderException => loaderException is not null)
                .Select(static loaderException => loaderException!.Message)
                .ToArray();
            throw new InvalidOperationException(
                $"Failed to inspect discoverable test types in '{assembly.Location}'. Loader exceptions: {string.Join(" | ", loaderMessages)}",
                exception);
        }
    }

    private static bool IsDiscoverableTestClass(Type type)
    {
        return type is { IsClass: true, IsPublic: true, IsAbstract: false }
               && type.GetCustomAttributes(inherit: false).Any(static attribute => attribute is TestClassAttribute);
    }

    private static bool IsDiscoverableTestMethod(MethodInfo method)
    {
        return method is { IsPublic: true, IsStatic: false }
               && method.GetCustomAttributes(inherit: false).Any(static attribute => attribute is TestMethodAttribute);
    }

    private static string[] GetTestCategories(IEnumerable<object> attributes)
    {
        return ToDistinctArray(
            attributes
                .OfType<TestCategoryAttribute>()
                .SelectMany(static attribute => attribute.TestCategories)
                .Where(static category => !string.IsNullOrWhiteSpace(category)));
    }

    private static string[] ToDistinctArray(IEnumerable<string> values)
    {
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var value in values)
        {
            if (seen.Add(value))
                result.Add(value);
        }

        return [.. result];
    }
}

[ExcludeFromCodeCoverage]
public readonly record struct DiscoverableOnlineTestApiCategoryUsage(
    string ProjectName,
    string TestClassName,
    string TestMethodName,
    string[] Categories)
{
    public string DisplayName => $"{ProjectName}:{TestClassName}.{TestMethodName}";
}
