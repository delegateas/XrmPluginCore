using System.Collections.Generic;
using System.Linq;
using System.Text;
using XrmPluginCore.SourceGenerator.Helpers;
using XrmPluginCore.SourceGenerator.Models;
using static XrmPluginCore.SourceGenerator.CodeGeneration.Indent;

namespace XrmPluginCore.SourceGenerator.CodeGeneration;

/// <summary>
/// Generates the type-safe Request/Response classes and the ActionWrapper for a Custom API registration.
/// </summary>
internal static class CustomApiClassGenerator
{
	private static readonly HashSet<string> CSharpKeywords =
	[
		"abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
		"class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
		"enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
		"foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
		"long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
		"private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
		"sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true",
		"try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual",
		"void", "volatile", "while",
	];

	/// <summary>
	/// Generates a complete source file containing the Request, Response and ActionWrapper classes
	/// for a Custom API registration. Returns null when there is no handler method to wire up.
	/// </summary>
	public static string GenerateCustomApiClasses(CustomApiMetadata metadata)
	{
		if (string.IsNullOrEmpty(metadata.HandlerMethodName))
		{
			return null;
		}

		var sb = new StringBuilder(512 + ((metadata.RequestParameters.Count + metadata.ResponseProperties.Count) * 80));

		sb.Append(GetFileHeader(metadata.NullableAnnotationsEnabled));
		sb.AppendLine($"namespace {metadata.Namespace}");
		sb.AppendLine("{");

		if (metadata.HasRequest)
		{
			GenerateRequestClass(sb, metadata);
		}

		if (metadata.HasResponse)
		{
			GenerateResponseClass(sb, metadata);
		}

		GenerateActionWrapperClass(sb, metadata);

		sb.AppendLine("}");

		return sb.ToString();
	}

	public static string GenerateHintName(CustomApiMetadata metadata) => $"{metadata.UniqueId}.g.cs";

	private static void GenerateRequestClass(StringBuilder sb, CustomApiMetadata metadata)
	{
		sb.AppendLine($"{L1}/// <summary>");
		sb.AppendLine($"{L1}/// Type-safe request for the {metadata.ApiName} Custom API.");
		sb.AppendLine($"{L1}/// </summary>");
		sb.AppendLine($"{L1}[CompilerGenerated]");
		sb.AppendLine($"{L1}public sealed class {metadata.RequestClassName}");
		sb.AppendLine($"{L1}{{");

		foreach (var parameter in metadata.RequestParameters)
		{
			sb.AppendLine($"{L2}public {EffectiveType(parameter, metadata.NullableAnnotationsEnabled)} {parameter.PropertyName} {{ get; set; }}");
		}

		sb.AppendLine($"{L1}}}");
		sb.AppendLine();
	}

	private static void GenerateResponseClass(StringBuilder sb, CustomApiMetadata metadata)
	{
		sb.AppendLine($"{L1}/// <summary>");
		sb.AppendLine($"{L1}/// Type-safe response for the {metadata.ApiName} Custom API.");
		sb.AppendLine($"{L1}/// </summary>");
		sb.AppendLine($"{L1}[CompilerGenerated]");
		sb.AppendLine($"{L1}public sealed class {metadata.ResponseClassName}");
		sb.AppendLine($"{L1}{{");

		foreach (var property in metadata.ResponseProperties)
		{
			sb.AppendLine($"{L2}public {EffectiveType(property, metadata.NullableAnnotationsEnabled)} {property.PropertyName} {{ get; set; }}");
		}

		sb.AppendLine();

		var ctorParams = string.Join(", ", metadata.ResponseProperties.Select(p => $"{EffectiveType(p, metadata.NullableAnnotationsEnabled)} {ToParameterName(p.PropertyName)}"));
		sb.AppendLine($"{L2}public {metadata.ResponseClassName}({ctorParams})");
		sb.AppendLine($"{L2}{{");
		foreach (var property in metadata.ResponseProperties)
		{
			sb.AppendLine($"{L3}{property.PropertyName} = {ToParameterName(property.PropertyName)};");
		}
		sb.AppendLine($"{L2}}}");

		sb.AppendLine($"{L1}}}");
		sb.AppendLine();
	}

	private static void GenerateActionWrapperClass(StringBuilder sb, CustomApiMetadata metadata)
	{
		sb.AppendLine($"{L1}/// <summary>");
		sb.AppendLine($"{L1}/// Generated action wrapper for {metadata.ServiceTypeName}.{metadata.HandlerMethodName}");
		sb.AppendLine($"{L1}/// </summary>");
		sb.AppendLine($"{L1}[CompilerGenerated]");
		sb.AppendLine($"{L1}internal sealed class {metadata.ActionWrapperClassName} : IActionWrapper");
		sb.AppendLine($"{L1}{{");
		sb.AppendLine($"{L2}public Action<IExtendedServiceProvider> CreateAction()");
		sb.AppendLine($"{L2}{{");
		sb.AppendLine($"{L3}return serviceProvider =>");
		sb.AppendLine($"{L3}{{");
		sb.AppendLine($"{L4}var service = serviceProvider.GetRequiredService<{metadata.ServiceTypeFullName}>();");
		sb.AppendLine($"{L4}var context = serviceProvider.GetRequiredService<IPluginExecutionContext>();");

		if (metadata.HasRequest)
		{
			sb.AppendLine();
			sb.AppendLine($"{L4}var request = new {metadata.RequestClassName}");
			sb.AppendLine($"{L4}{{");
			foreach (var parameter in metadata.RequestParameters)
			{
				var castType = EffectiveType(parameter, metadata.NullableAnnotationsEnabled);
				sb.AppendLine(
					$"{L5}{parameter.PropertyName} = context.InputParameters.Contains(\"{parameter.UniqueName}\") " +
					$"? ({castType})context.InputParameters[\"{parameter.UniqueName}\"] : default,");
			}
			sb.AppendLine($"{L4}}};");
		}

		sb.AppendLine();

		var argument = metadata.HasRequest ? "request" : string.Empty;
		if (metadata.HasResponse)
		{
			sb.AppendLine($"{L4}var response = service.{metadata.HandlerMethodName}({argument});");
			sb.AppendLine();
			foreach (var property in metadata.ResponseProperties)
			{
				sb.AppendLine($"{L4}context.OutputParameters[\"{property.UniqueName}\"] = response.{property.PropertyName};");
			}
		}
		else
		{
			sb.AppendLine($"{L4}service.{metadata.HandlerMethodName}({argument});");
		}

		sb.AppendLine($"{L3}}};");
		sb.AppendLine($"{L2}}}");
		sb.AppendLine($"{L1}}}");
	}

	/// <summary>
	/// The CLR type to emit for a parameter. Reference types get a nullable <c>?</c> only when NRT
	/// annotations are enabled (so the output stays valid on NRT-off / C# 7.3 consumers); value types
	/// already carry their own <c>?</c> when optional.
	/// </summary>
	private static string EffectiveType(CustomApiParameterMetadata parameter, bool nullableEnabled)
	{
		if (CustomApiParameterTypeMapper.IsValueType(parameter.ParameterType))
		{
			return parameter.ClrType;
		}

		return nullableEnabled ? parameter.ClrType + "?" : parameter.ClrType;
	}

	private static string ToParameterName(string propertyName)
	{
		if (string.IsNullOrEmpty(propertyName))
		{
			return propertyName;
		}

		var camelCase = char.ToLowerInvariant(propertyName[0]) + propertyName.Substring(1);

		// Escape keywords (and the rare case where lowercasing collided with a keyword) with a verbatim @.
		return CSharpKeywords.Contains(camelCase) ? "@" + camelCase : camelCase;
	}

	private static string GetFileHeader(bool nullableEnabled) =>
$"""
// <auto-generated />
{NullableHelper.FileDirective(nullableEnabled)}
using System;
using System.Runtime.CompilerServices;
using Microsoft.Xrm.Sdk;
using Microsoft.Extensions.DependencyInjection;
using XrmPluginCore;

""";
}
