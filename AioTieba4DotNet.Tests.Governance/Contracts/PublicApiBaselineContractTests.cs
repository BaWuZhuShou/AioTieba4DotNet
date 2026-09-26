#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using AioTieba4DotNet.Tests.Platform.Contracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AioTieba4DotNet.Tests.Governance.Contracts;

[TestClass]
[TestCategory(OnlineTestContractCategories.Architecture)]
public sealed class PublicApiBaselineContractTests
{
    [TestMethod]
    public void ProductPublicMetadataMatchesReviewedBaseline()
    {
        using var stream = typeof(PublicApiBaselineContractTests).Assembly.GetManifestResourceStream(
            "AioTieba4DotNet.Tests.Governance.Contracts.Baselines.public-api.txt");
        Assert.IsNotNull(stream, "必须嵌入已经评审的公开 API 基线；测试不会生成或接受基线。");
        using var reader = new StreamReader(stream);
        var expected = reader.ReadToEnd();
        var actual = PublicApiSnapshot.Create(typeof(TiebaClient).Assembly);

        Assert.IsTrue(expected.Length > 0, "公开 API 基线不能为空。");
        Assert.IsNull(PublicApiSnapshot.Difference(expected, actual));
    }

    [TestMethod]
    public void SnapshotDistinguishesDefaultsNullabilityAccessorsAndParameterModifiers()
    {
        var snapshot = PublicApiSnapshot.Create([typeof(SignatureFixture)]);
        var method = Line(snapshot, "method ", " Parameters ");
        StringAssert.Contains(method, "value:System.Int32& in=False out=False optional=False");
        StringAssert.Contains(method, "input:System.Int32& in=True out=False");
        StringAssert.Contains(method, "output:System.Int32& in=False out=True");
        StringAssert.Contains(method, "count:System.Int32 in=False out=False optional=True default=7");
        StringAssert.Contains(method, "text:System.String in=False out=False optional=True default=\"a\\nb\"");
        StringAssert.Contains(method, "cancellationToken:System.Threading.CancellationToken in=False out=False optional=True default=null");
        StringAssert.Contains(Line(snapshot, "method ", " Many "), "System.ParamArrayAttribute");
        StringAssert.Contains(Line(snapshot, "method ", " Price "), "default=1.25");
        StringAssert.Contains(Line(snapshot, "method ", " Returns "), "returns=<return>:System.String");
        StringAssert.Contains(Line(snapshot, "property ", ".NullableItems "), "nullability=NotNull/NotNull<Nullable/Nullable>");
        StringAssert.Contains(Line(snapshot, "property ", ".NonNullableItems "), "nullability=NotNull/NotNull<NotNull/NotNull>");
        StringAssert.Contains(Line(snapshot, "property ", ".Restricted "), "| set=private");
        StringAssert.Contains(Line(snapshot, "property ", ".Initial "), "System.Runtime.CompilerServices.IsExternalInit");
        StringAssert.Contains(Line(snapshot, "property ", ".Item "), "index=(index:System.Int32");
        StringAssert.Contains(Line(snapshot, "property ", ".Serialized "), "System.Text.Json.Serialization.JsonPropertyNameAttribute(System.String:\"wire_name\")");
        StringAssert.Contains(Line(snapshot, "property ", ".Serialized "), "System.Text.Json.Serialization.JsonIgnoreAttribute(){property:Condition=System.Text.Json.Serialization.JsonIgnoreCondition:3}");
    }

    [TestMethod]
    public void SnapshotIncludesGenericConstraintsInheritanceVisibilityEventsAndConstants()
    {
        var snapshot = PublicApiSnapshot.Create([
            typeof(GenericFixture<>), typeof(GenericFixture<>.Nested<>), typeof(FixtureEnum), typeof(SignatureFixture),
            typeof(SignatureFixture).GetNestedType("ProtectedNested", BindingFlags.NonPublic)!, typeof(IProducer<>)
        ]);
        StringAssert.Contains(Line(snapshot, "type ", "+GenericFixture`1<"), "!T:ReferenceTypeConstraint, DefaultConstructorConstraint constraints=[System.IDisposable]");
        StringAssert.Contains(Line(snapshot, "type ", "+Nested`1<"), "declaring=AioTieba4DotNet.Tests.Governance.Contracts.PublicApiBaselineContractTests+GenericFixture`1<!T>");
        StringAssert.Contains(Line(snapshot, "type ", "+IProducer`1<"), "!T:Covariant");
        StringAssert.Contains(Line(snapshot, "type ", "+FixtureEnum "), "underlying=System.UInt64");
        StringAssert.Contains(Line(snapshot, "field ", ".Maximum "), "value=18446744073709551615");
        StringAssert.Contains(Line(snapshot, "field ", ".Answer "), "literal=True");
        StringAssert.Contains(Line(snapshot, "field ", ".Answer "), "value=42");
        StringAssert.Contains(Line(snapshot, "field ", ".State "), "protected internal");
        StringAssert.Contains(Line(snapshot, "ctor ", "+SignatureFixture "), "protected .ctor");
        StringAssert.Contains(Line(snapshot, "event ", ".Changed "), "add={public add_Changed");
        StringAssert.Contains(Line(snapshot, "method ", " op_Addition "), "static=True");
        StringAssert.Contains(Line(snapshot, "method ", " Convert "), "!!TResult:NotNullableValueTypeConstraint, DefaultConstructorConstraint");
        StringAssert.Contains(Line(snapshot, "method ", " Convert "), "values:System.String[,]");
        StringAssert.Contains(Line(snapshot, "method ", " Returns "), "virtual=True");
        StringAssert.Contains(Line(snapshot, "type ", "+ProtectedNested "), "nested protected");
        Assert.IsFalse(snapshot.Contains("Hidden", StringComparison.Ordinal));
    }

    [TestMethod]
    public void SnapshotIsCultureAndInputOrderIndependentAndReportsMetadataDifferences()
    {
        var types = new[] { typeof(SignatureFixture), typeof(FixtureEnum) };
        var originalCulture = CultureInfo.CurrentCulture;
        string expected;
        string actual;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            expected = PublicApiSnapshot.Create(types);
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            actual = PublicApiSnapshot.Create(types.Reverse());
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }

        Assert.IsNull(PublicApiSnapshot.Difference(expected, actual));
        Assert.IsNull(PublicApiSnapshot.Difference(expected.Replace("\n", "\r\n", StringComparison.Ordinal), actual));
        foreach (var (before, after) in new[]
                 {
                     ("default=7", "default=8"), ("value=42", "value=43"),
                     ("NotNull/NotNull<Nullable/Nullable>", "NotNull/NotNull<NotNull/NotNull>"),
                     ("| set=private", "| set=internal"), ("in=True", "in=False")
                 })
        {
            Assert.IsNotNull(PublicApiSnapshot.Difference(expected, actual.Replace(before, after, StringComparison.Ordinal)), before);
        }

        Assert.IsNotNull(PublicApiSnapshot.Difference(expected, actual + "new member\n"));
        Assert.IsNotNull(PublicApiSnapshot.Difference(expected, ""));
    }

    [TestMethod]
    public void SnapshotDistinguishesNullabilityInBaseInterfacesAndGenericConstraints()
    {
        foreach (var (nullable, required) in new[]
                 {
                     (typeof(NullableBase), typeof(RequiredBase)),
                     (typeof(INullableBase), typeof(IRequiredBase)),
                     (typeof(NullableConstraint<>), typeof(RequiredConstraint<>)),
                     (typeof(NullableMethodConstraint), typeof(RequiredMethodConstraint))
                 })
        {
            // 仅统一夹具名称；两个真实 C# 声明的唯一签名差别是 string? / string。
            var nullableSnapshot = PublicApiSnapshot.Create([nullable]).Replace(nullable.Name, "Shape", StringComparison.Ordinal);
            var requiredSnapshot = PublicApiSnapshot.Create([required]).Replace(required.Name, "Shape", StringComparison.Ordinal);
            Assert.IsNotNull(PublicApiSnapshot.Difference(nullableSnapshot, requiredSnapshot), nullable.Name);
        }

        StringAssert.Contains(PublicApiSnapshot.Create([typeof(NullableBase)]),
            "System.Runtime.CompilerServices.NullableAttribute(System.Byte[]:[System.Byte:0,System.Byte:2])");
        StringAssert.Contains(PublicApiSnapshot.Create([typeof(INullableBase)]),
            "interface-nullability=[AioTieba4DotNet.Tests.Governance.Contracts.PublicApiBaselineContractTests+IProducer`1<System.String>:0100");
        StringAssert.Contains(PublicApiSnapshot.Create([typeof(NullableConstraint<>)]),
            "constraint-nullability=[System.Collections.Generic.IEnumerable`1<System.String>:0100");
        StringAssert.Contains(PublicApiSnapshot.Create([typeof(AioTieba4DotNet.Models.Forums.RoomList)]),
            "System.Runtime.CompilerServices.NullableAttribute");
    }

    private static string Line(string snapshot, string prefix, string fragment) => snapshot.Split('\n')
        .Single(line => line.StartsWith(prefix, StringComparison.Ordinal) &&
                        (prefix == "type " ? line.Split('|')[0] : line).Contains(fragment, StringComparison.Ordinal));

    public class SignatureFixture
    {
        protected SignatureFixture() { }
        public const int Answer = 42;
        protected internal readonly int State;
        private protected int HiddenField;
        protected class ProtectedNested { }
        private class HiddenNested { }
        public List<string?> NullableItems { get; } = [];
        public List<string> NonNullableItems { get; } = [];
        public string Restricted { get; private set; } = "";
        public string Initial { get; init; } = "";
        public string this[int index] => index.ToString(CultureInfo.InvariantCulture);
        [JsonPropertyName("wire_name")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Serialized { get; set; }
        public event EventHandler? Changed { add { } remove { } }
        public static SignatureFixture operator +(SignatureFixture left, SignatureFixture right) => left;
        public virtual string? Returns() => null;
        public static void Parameters(ref int value, in int input, out int output, int count = 7,
            string? text = "a\nb", System.Threading.CancellationToken cancellationToken = default) => output = input;
        public static void Many(params string[] values) { }
        public static decimal Price(decimal amount = 1.25m) => amount;
        public static TResult Convert<TResult>(string[,] values) where TResult : struct => default;
    }

    public class GenericFixture<T> where T : class, IDisposable, new()
    {
        public class Nested<TValue> where TValue : unmanaged { }
    }

    public interface IProducer<out T> { T Get(); }
    public enum FixtureEnum : ulong { Zero = 0, Maximum = ulong.MaxValue }
    public sealed class NullableBase : List<string?> { }
    public sealed class RequiredBase : List<string> { }
    public interface INullableBase : IProducer<string?> { }
    public interface IRequiredBase : IProducer<string> { }
    public sealed class NullableConstraint<T> where T : IEnumerable<string?> { }
    public sealed class RequiredConstraint<T> where T : IEnumerable<string> { }
    public sealed class NullableMethodConstraint
    {
        public static void Handle<T>() where T : IEnumerable<string?> { }
    }
    public sealed class RequiredMethodConstraint
    {
        public static void Handle<T>() where T : IEnumerable<string> { }
    }
}
