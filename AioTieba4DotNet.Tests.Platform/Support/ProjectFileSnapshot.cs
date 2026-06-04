#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace AioTieba4DotNet.Tests.Platform.Support;

[ExcludeFromCodeCoverage]
public sealed class ProjectFileSnapshot
{
    public string ProjectName { get; }

    public string RawText { get; }

    public XDocument Document { get; }

    private ProjectFileSnapshot(string projectName, string rawText, XDocument document)
    {
        ProjectName = projectName;
        RawText = rawText;
        Document = document;
    }

    public string[] PropertyNames =>
        Document.Root?.Elements("PropertyGroup")
            .Elements()
            .Select(static element => element.Name.LocalName)
            .ToArray()
        ?? [];

    public string[] PackageReferences => GetItems("PackageReference", static element => element.Attribute("Include")?.Value);

    public string[] ProjectReferences => GetItems("ProjectReference", static element => element.Attribute("Include")?.Value);

    public string[] CompileIncludes => GetItems("Compile", static element => element.Attribute("Include")?.Value);

    public string[] CompileRemovals => GetItems("Compile", static element => element.Attribute("Remove")?.Value);

    public bool HasProperty(string propertyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
        return PropertyNames.Contains(propertyName, StringComparer.Ordinal);
    }

    public static ProjectFileSnapshot Load(string projectName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectName);

        var projectPath = RepositoryPaths.GetProjectFilePath(projectName);
        var rawText = File.ReadAllText(projectPath);
        var document = XDocument.Parse(rawText, LoadOptions.PreserveWhitespace);

        return new ProjectFileSnapshot(projectName, rawText, document);
    }

    private string[] GetItems(string elementName, Func<XElement, string?> selector)
    {
        IEnumerable<XElement> elements = Document.Root?.Descendants(elementName) ?? [];

        return elements
            .Select(selector)
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value!)
            .ToArray();
    }
}
