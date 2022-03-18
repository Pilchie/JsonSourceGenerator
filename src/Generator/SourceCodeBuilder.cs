using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using Generator.Extensions;

using Microsoft.CodeAnalysis;

namespace Generator;

public class SourceCodeBuilder
{
    private DiagnosticDescriptor UnsupportedPropertyType { get; } = new DiagnosticDescriptor(
        id: "JSG001",
        title: "Unsupported property type.",
        messageFormat: "Property type {0} is not supproted.",
        category: "SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private readonly Action<Diagnostic> _reportDiagnostic;

    public SourceCodeBuilder(Action<Diagnostic> reportDiagnostic)
    {
        _reportDiagnostic = reportDiagnostic ?? throw new ArgumentNullException(nameof(reportDiagnostic));
    }


    public bool TryGenerateSource(
        INamedTypeSymbol interfaceTypeToWrap,
        [NotNullWhen(true)] out string? content)
    {
        using var codeTextWriter = new SourceCodeWriter();
        content = null;

        if (TryGenerateSource(codeTextWriter, interfaceTypeToWrap))
        {
            content = codeTextWriter.ToString();
            return true;
        }

        return false;
    }

    private bool TryGenerateSource(
        SourceCodeWriter output,
        INamedTypeSymbol interfaceTypeToWrap)
    {
        using (output.WriteOutNamespace(interfaceTypeToWrap))
        {
            // Handle nested class
            var indentations = ImmutableArray<IDisposable>.Empty;
            if (!interfaceTypeToWrap.ContainingSymbol.Equals(interfaceTypeToWrap.ContainingNamespace, SymbolEqualityComparer.Default))
            {
                var containingTypes = interfaceTypeToWrap.GetContainingTypes().ToImmutableArray();
                var builder =  ImmutableArray.CreateBuilder<IDisposable>(containingTypes.Length);
                foreach (var containingType in containingTypes.Reverse())
                {
                    builder.Add(WriteOutTypeDeclarations(output, containingType));
                }

                indentations = builder.MoveToImmutable();
            }

            // Write out type declaration
            using (WriteOutTypeDeclarations(output, interfaceTypeToWrap))
            {
                foreach (var symbol in interfaceTypeToWrap.GetMembers().Where(s => s.Kind == SymbolKind.Property))
                {
                    if (symbol is IPropertySymbol propertySymbol)
                    {
                        if (!TryWriteOutProperty(output, propertySymbol, location => _reportDiagnostic(Diagnostic.Create(UnsupportedPropertyType, location))))
                        {
                            return false;
                        }
                    }
                }
            }

            foreach (var block in indentations)
            {
                block.Dispose();
            }

            return true;
        }
    }

    private static IDisposable WriteOutTypeDeclarations(
        SourceCodeWriter output,
        INamedTypeSymbol interfaceTypeToWrap)
    {
        var typeName = interfaceTypeToWrap.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        using (output.WriteOutTypeDeclaration("public", "static", "class", typeName + "_JsonWrapperExtensions", ImmutableArray<string>.Empty, Array.Empty<string>()))
        {
            using (output.WriteBlock($"public static {typeName} As{typeName}(this System.Text.Json.JsonElement element)"))
            {
                output.WriteLine($"return new {typeName}_JsonWrapper(element);");
            }
        }

        output.WriteNewLine();
        var block = output.WriteOutTypeDeclaration("internal", string.Empty, "class", typeName + "_JsonWrapper", ImmutableArray<string>.Empty, new[] { typeName });
        output.WriteLine("private readonly System.Text.Json.JsonElement _element;");
        output.WriteNewLine();
        using (output.WriteBlock($"public {typeName}_JsonWrapper(System.Text.Json.JsonElement element)"))
        {
            output.WriteLine("_element = element;");
        }
        return block;
    }

    private static bool TryWriteOutProperty(SourceCodeWriter output, IPropertySymbol propertySymbol, Action<Location?> reportDiagnostic)
    {
        // get the name and type of the field
        var propertyName = propertySymbol.Name;
        var propertyType = propertySymbol.Type.ToDisplayString();
        var conversionMethod = propertySymbol.Type.SpecialType switch
        {
            SpecialType.System_Boolean => "GetBoolean",
            SpecialType.System_SByte => "GetSByte",
            SpecialType.System_Byte => "GetByte",
            SpecialType.System_Int16 => "GetInt16",
            SpecialType.System_UInt16 => "GetUInt16",
            SpecialType.System_Int32 => "GetInt32",
            SpecialType.System_UInt32 => "GetUInt32",
            SpecialType.System_Int64 => "GetInt64",
            SpecialType.System_UInt64 => "GetUInt64",
            SpecialType.System_Decimal => "GetDecimal",
            SpecialType.System_Single => "GetSingle",
            SpecialType.System_Double => "GetDouble",
            SpecialType.System_String => "GetString",
            SpecialType.System_DateTime => "GetDateTime",

            // TODO: Handle "BytesFromBase64", DateTimeOffset, Guid

            // TODO: Unwrap and allow
            SpecialType.System_Array => throw new NotImplementedException(),

            // TODO: Unwrap and allow (use TryGet and missing property for value types?)
            SpecialType.System_Nullable_T => throw new NotImplementedException(),

            // TODO: Test that this is an interface with the attribute and get a wrapper for it.
            SpecialType.None => throw new NotImplementedException(),

            _ => null,
        };

        if (conversionMethod is null)
        {
            reportDiagnostic(propertySymbol.DeclaringSyntaxReferences.First().SyntaxTree.GetLocation(propertySymbol.DeclaringSyntaxReferences.First().Span));
            return false;
        }

        output.WriteOutProperty(propertyName, propertyType, conversionMethod);
        return true;
    }
}
