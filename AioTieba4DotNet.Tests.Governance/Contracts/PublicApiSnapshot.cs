#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text.Json;

namespace AioTieba4DotNet.Tests.Governance.Contracts;

// 只读取元数据；不实例化产品类型或属性，也不自动更新预期快照。
internal static class PublicApiSnapshot
{
    private const BindingFlags DeclaredMembers = BindingFlags.DeclaredOnly | BindingFlags.Public |
                                                 BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    internal static string Create(Assembly assembly) => Create(assembly.GetTypes());

    internal static string Create(IEnumerable<Type> types)
    {
        var nullability = new NullabilityInfoContext();
        var lines = new List<string>();
        foreach (var type in types.Where(IsExternallyVisible))
        {
            var owner = TypeName(type);
            var kind = type.IsEnum ? "enum" : type.IsInterface ? "interface" : type.IsValueType ? "struct" :
                typeof(MulticastDelegate).IsAssignableFrom(type) ? "delegate" : "class";
            lines.Add($"type {owner} | {TypeVisibility(type)} {kind} abstract={type.IsAbstract} sealed={type.IsSealed}" +
                      $" | base={TypeName(type.BaseType)} | interfaces={Join(type.GetInterfaces().Select(TypeName))}" +
                      $" | interface-nullability=[{RelationshipNullability(type)}]" +
                      $" | declaring={TypeName(type.DeclaringType)} | generic={GenericParameters(type.GetGenericArguments())}" +
                      (type.IsEnum ? $" | underlying={TypeName(Enum.GetUnderlyingType(type))}" : "") +
                      Attributes(type.CustomAttributes, includeNullableEncoding: true));

            foreach (var constructor in type.GetConstructors(DeclaredMembers).Where(IsAccessible))
            {
                lines.Add($"ctor {owner} | {MethodSignature(constructor, nullability)}");
            }

            var accessors = type.GetProperties(DeclaredMembers).SelectMany(property => property.GetAccessors(true))
                .Concat(type.GetEvents(DeclaredMembers).SelectMany(@event =>
                    new[] { @event.AddMethod, @event.RemoveMethod, @event.RaiseMethod }.OfType<MethodInfo>()))
                .ToHashSet();
            foreach (var method in type.GetMethods(DeclaredMembers).Where(IsAccessible).Where(method => !accessors.Contains(method)))
            {
                lines.Add($"method {owner} | {MethodSignature(method, nullability)}");
            }

            foreach (var property in type.GetProperties(DeclaredMembers).Where(property => property.GetAccessors(true).Any(IsAccessible)))
            {
                lines.Add($"property {owner}.{property.Name} | type={TypeName(property.PropertyType)}" +
                          $" | nullability={Nullability(nullability.Create(property))}" +
                          $" | index=({Parameters(property.GetIndexParameters(), nullability)})" +
                          Modifiers(property.GetRequiredCustomModifiers(), property.GetOptionalCustomModifiers()) +
                          $" | get={Accessor(property.GetMethod, nullability)} | set={Accessor(property.SetMethod, nullability)}" +
                          Attributes(property.CustomAttributes));
            }

            foreach (var @event in type.GetEvents(DeclaredMembers).Where(@event =>
                         new[] { @event.AddMethod, @event.RemoveMethod, @event.RaiseMethod }.OfType<MethodInfo>().Any(IsAccessible)))
            {
                lines.Add($"event {owner}.{@event.Name} | type={TypeName(@event.EventHandlerType)}" +
                          $" | nullability={Nullability(nullability.Create(@event))}" +
                          $" | add={Accessor(@event.AddMethod, nullability)} | remove={Accessor(@event.RemoveMethod, nullability)}" +
                          $" | raise={Accessor(@event.RaiseMethod, nullability)}" + Attributes(@event.CustomAttributes));
            }

            foreach (var field in type.GetFields(DeclaredMembers).Where(IsAccessible))
            {
                lines.Add($"field {owner}.{field.Name} | {Visibility(field.Attributes & FieldAttributes.FieldAccessMask)}" +
                          $" static={field.IsStatic} readonly={field.IsInitOnly} literal={field.IsLiteral}" +
                          $" | type={TypeName(field.FieldType)} | nullability={Nullability(nullability.Create(field))}" +
                          Modifiers(field.GetRequiredCustomModifiers(), field.GetOptionalCustomModifiers()) +
                          (field.IsLiteral ? $" | value={Constant(field.GetRawConstantValue())}" : "") + Attributes(field.CustomAttributes));
            }
        }

        return string.Join('\n', lines.Order(StringComparer.Ordinal)) + "\n";
    }

    // 返回首处差异，避免把数千行快照倾倒进测试输出。
    internal static string? Difference(string expected, string actual)
    {
        var expectedLines = expected.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var actualLines = actual.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        for (var index = 0; index < Math.Max(expectedLines.Length, actualLines.Length); index++)
        {
            var expectedLine = index < expectedLines.Length ? expectedLines[index] : "<EOF>";
            var actualLine = index < actualLines.Length ? actualLines[index] : "<EOF>";
            if (!string.Equals(expectedLine, actualLine, StringComparison.Ordinal))
            {
                return $"公开 API 基线第 {index + 1} 行存在差异。\n预期: {expectedLine}\n实际: {actualLine}";
            }
        }

        return null;
    }

    private static string MethodSignature(MethodBase method, NullabilityInfoContext nullability)
    {
        var result = $"{Visibility(method.Attributes & MethodAttributes.MemberAccessMask)} {method.Name}" +
                     $" static={method.IsStatic} abstract={method.IsAbstract} virtual={method.IsVirtual} final={method.IsFinal}" +
                     $" newslot={(method.Attributes & MethodAttributes.NewSlot) != 0} calling={method.CallingConvention}" +
                     $" generic={GenericParameters(method.IsGenericMethod ? method.GetGenericArguments() : [])}" +
                     $" parameters=({Parameters(method.GetParameters(), nullability)})";
        if (method is MethodInfo info)
        {
            result += $" returns={Parameter(info.ReturnParameter, nullability)}";
        }

        return result + Attributes(method.CustomAttributes, includeNullableEncoding: method.IsGenericMethod);
    }

    private static string Accessor(MethodInfo? method, NullabilityInfoContext nullability) =>
        method is null ? "none" : IsAccessible(method) ? "{" + MethodSignature(method, nullability) + "}" : Visibility(method.Attributes & MethodAttributes.MemberAccessMask);

    private static string Parameters(IEnumerable<ParameterInfo> parameters, NullabilityInfoContext nullability) =>
        string.Join("; ", parameters.Select(parameter => Parameter(parameter, nullability)));

    private static string Parameter(ParameterInfo parameter, NullabilityInfoContext nullability) =>
        $"{(parameter.Position < 0 ? "<return>" : parameter.Name)}:{TypeName(parameter.ParameterType)}" +
        $" in={parameter.IsIn} out={parameter.IsOut} optional={parameter.IsOptional}" +
        $" default={(parameter.HasDefaultValue ? Constant(parameter.DefaultValue) : "<none>")}" +
        $" nullability={Nullability(nullability.Create(parameter))}" +
        Modifiers(parameter.GetRequiredCustomModifiers(), parameter.GetOptionalCustomModifiers()) + Attributes(parameter.CustomAttributes);

    private static string GenericParameters(IEnumerable<Type> arguments) => string.Join("; ", arguments
        .Where(argument => argument.IsGenericParameter)
        .Select(argument => $"{TypeName(argument)}:{argument.GenericParameterAttributes}" +
                            $" constraints=[{Join(argument.GetGenericParameterConstraints().Select(TypeName))}]" +
                            $" constraint-nullability=[{RelationshipNullability(argument)}]" +
                            Attributes(argument.CustomAttributes, includeNullableEncoding: true)));

    // Reflection 没有暴露 InterfaceImpl / GenericParamConstraint 行上的 NullableAttribute。
    // 仅保存这些行的可空性属性编码；不把会随编译变化的 metadata token 写入文本。
    private static string RelationshipNullability(Type type)
    {
        using var stream = File.OpenRead(type.Module.FullyQualifiedName);
        using var pe = new PEReader(stream);
        var reader = pe.GetMetadataReader();
        var entries = new List<string>();
        if (type.IsGenericParameter)
        {
            var parameter = reader.GetGenericParameter((GenericParameterHandle)MetadataTokens.Handle(type.MetadataToken));
            foreach (var handle in parameter.GetConstraints())
            {
                var constraint = reader.GetGenericParameterConstraint(handle);
                Add(constraint.Type, constraint.GetCustomAttributes());
            }
        }
        else
        {
            var definition = reader.GetTypeDefinition((TypeDefinitionHandle)MetadataTokens.Handle(type.MetadataToken));
            foreach (var handle in definition.GetInterfaceImplementations())
            {
                var implementation = reader.GetInterfaceImplementation(handle);
                Add(implementation.Interface, implementation.GetCustomAttributes());
            }
        }

        return Join(entries);

        void Add(EntityHandle target, CustomAttributeHandleCollection attributes)
        {
            foreach (var handle in attributes)
            {
                var attribute = reader.GetCustomAttribute(handle);
                var attributeType = attribute.Constructor.Kind == HandleKind.MemberReference
                    ? reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor).Parent
                    : reader.GetMethodDefinition((MethodDefinitionHandle)attribute.Constructor).GetDeclaringType();
                var name = attributeType.Kind switch
                {
                    HandleKind.TypeReference => reader.GetString(reader.GetTypeReference((TypeReferenceHandle)attributeType).Namespace) + "." +
                                                reader.GetString(reader.GetTypeReference((TypeReferenceHandle)attributeType).Name),
                    HandleKind.TypeDefinition => reader.GetString(reader.GetTypeDefinition((TypeDefinitionHandle)attributeType).Namespace) + "." +
                                                 reader.GetString(reader.GetTypeDefinition((TypeDefinitionHandle)attributeType).Name),
                    _ => ""
                };
                if (name != "System.Runtime.CompilerServices.NullableAttribute") continue;
                var arguments = type.IsGenericParameter ? type.DeclaringType!.GetGenericArguments() : type.GetGenericArguments();
                var methodArguments = type.IsGenericParameter ? type.DeclaringMethod?.GetGenericArguments() : null;
                var resolved = type.Module.ResolveType(MetadataTokens.GetToken(target), arguments, methodArguments);
                entries.Add(TypeName(resolved) + ":" + Convert.ToHexString(reader.GetBlobBytes(attribute.Value)));
            }
        }
    }

    private static string Nullability(NullabilityInfo info) =>
        $"{info.ReadState}/{info.WriteState}" +
        (info.ElementType is null ? "" : $"[{Nullability(info.ElementType)}]") +
        (info.GenericTypeArguments.Length == 0 ? "" : $"<{string.Join(",", info.GenericTypeArguments.Select(Nullability))}>");

    private static string TypeName(Type? type)
    {
        if (type is null) return "none";
        if (type.IsGenericParameter) return (type.DeclaringMethod is null ? "!" : "!!") + type.Name;
        if (type.IsByRef) return TypeName(type.GetElementType()) + "&";
        if (type.IsPointer) return TypeName(type.GetElementType()) + "*";
        if (type.IsArray)
        {
            return TypeName(type.GetElementType()) + (type.IsSZArray ? "[]" :
                type.GetArrayRank() == 1 ? "[*]" : "[" + new string(',', type.GetArrayRank() - 1) + "]");
        }

        if (type.IsFunctionPointer)
        {
            throw new NotSupportedException("公开函数指针尚未纳入快照格式；必须先扩展格式及有效性测试。");
        }

        return type.IsGenericType
            ? type.GetGenericTypeDefinition().FullName + "<" + string.Join(",", type.GetGenericArguments().Select(TypeName)) + ">"
            : type.FullName ?? throw new InvalidOperationException("公开签名类型缺少完全限定名。");
    }

    private static string Modifiers(Type[] required, Type[] optional) =>
        $" modreq=[{string.Join(",", required.Select(TypeName))}] modopt=[{string.Join(",", optional.Select(TypeName))}]";

    private static string Attributes(IEnumerable<CustomAttributeData> attributes, bool includeNullableEncoding = false)
    {
        var values = attributes.Where(attribute => IncludeAttribute(attribute.AttributeType, includeNullableEncoding)).Select(attribute =>
            TypeName(attribute.AttributeType) + "(" + string.Join(",", attribute.ConstructorArguments.Select(AttributeArgument)) + ")" +
            "{" + Join(attribute.NamedArguments.Select(argument =>
                (argument.IsField ? "field:" : "property:") + argument.MemberName + "=" + AttributeArgument(argument.TypedValue))) + "}");
        return $" attrs=[{Join(values)}]";
    }

    private static bool IncludeAttribute(Type type, bool includeNullableEncoding) => type.FullName switch
    {
        "System.Runtime.CompilerServices.NullableAttribute" or "System.Runtime.CompilerServices.NullableContextAttribute" => includeNullableEncoding,
        "System.Runtime.CompilerServices.CompilerGeneratedAttribute" or
        "System.Runtime.CompilerServices.AsyncStateMachineAttribute" or
        "System.Runtime.CompilerServices.IteratorStateMachineAttribute" or
        "System.Runtime.CompilerServices.AsyncIteratorStateMachineAttribute" or
        "System.Diagnostics.DebuggerHiddenAttribute" or
        "System.Diagnostics.DebuggerStepThroughAttribute" or
        "System.Diagnostics.DebuggerNonUserCodeAttribute" or
        "System.Diagnostics.DebuggerDisplayAttribute" or
        "System.Diagnostics.DebuggerBrowsableAttribute" or
        "System.Diagnostics.CodeAnalysis.SuppressMessageAttribute" or
        "System.CodeDom.Compiler.GeneratedCodeAttribute" => false,
        _ => true
    };

    private static string AttributeArgument(CustomAttributeTypedArgument argument) =>
        TypeName(argument.ArgumentType) + ":" + (argument.Value is IEnumerable<CustomAttributeTypedArgument> items
            ? "[" + string.Join(",", items.Select(AttributeArgument)) + "]"
            : Constant(argument.Value));

    private static string Constant(object? value) => value switch
    {
        null => "null",
        string text => JsonSerializer.Serialize(text),
        char character => $"char:{(int)character}",
        bool boolean => boolean ? "true" : "false",
        Type type => "typeof(" + TypeName(type) + ")",
        Enum enumeration => TypeName(enumeration.GetType()) + ":" + enumeration.ToString("D"),
        float number => number.ToString("R", CultureInfo.InvariantCulture),
        double number => number.ToString("R", CultureInfo.InvariantCulture),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        Missing => "<missing>",
        DBNull => "<dbnull>",
        _ => throw new NotSupportedException($"未支持的元数据常量类型：{value.GetType().FullName}")
    };

    private static bool IsExternallyVisible(Type type) => type.IsNested
        ? (type.IsNestedPublic || type.IsNestedFamily || type.IsNestedFamORAssem) && IsExternallyVisible(type.DeclaringType!)
        : type.IsPublic;

    private static bool IsAccessible(MethodBase method) => method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly;
    private static bool IsAccessible(FieldInfo field) => field.IsPublic || field.IsFamily || field.IsFamilyOrAssembly;

    private static string TypeVisibility(Type type) => !type.IsNested ? "public" : type.IsNestedPublic ? "nested public" :
        type.IsNestedFamily ? "nested protected" : "nested protected internal";

    private static string Visibility(MethodAttributes attributes) => attributes switch
    {
        MethodAttributes.Public => "public",
        MethodAttributes.Family => "protected",
        MethodAttributes.FamORAssem => "protected internal",
        MethodAttributes.FamANDAssem => "private protected",
        MethodAttributes.Assembly => "internal",
        _ => "private"
    };

    private static string Visibility(FieldAttributes attributes) => Visibility((MethodAttributes)(int)attributes);
    private static string Join(IEnumerable<string> values) => string.Join(",", values.Order(StringComparer.Ordinal));
}
