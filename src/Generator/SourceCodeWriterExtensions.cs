using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;

namespace Generator.Extensions;
internal static class SourceCodeWriterExtensions
{
    public static IDisposable WriteOutNamespace(this SourceCodeWriter output, INamedTypeSymbol type)
    {
        return output.WriteBlock($"namespace {type.ContainingNamespace.ToDisplayString()}");
    }

    public static IDisposable WriteOutTypeDeclaration(this SourceCodeWriter output,
        string accessibility,
        string modifiers,
        string typeKind,
        string typeName,
        ImmutableArray<string> inheritsList,
        string[] implementsList)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(accessibility))
        {
            sb.Append(accessibility);
        }

        if (!string.IsNullOrWhiteSpace(modifiers))
        {
            sb.Append(' ');
            sb.Append(modifiers);
        }

        sb.Append(' ');
        sb.Append(typeKind);
        sb.Append(' ');
        sb.Append(typeName);
        var inheritanceString = string.Join(", ", inheritsList.Concat(implementsList));

        if (!string.IsNullOrWhiteSpace(inheritanceString))
        {
            sb.Append(" : ");
            sb.Append(inheritanceString);
        }
        output.WriteLine(sb.ToString());
        return output.BeginWriteBlock();
    }

    public static void WriteOutProperty(this SourceCodeWriter output, string propertyName, string propertyType, string conversionMethod)
    {
        output.WriteNewLine();
        using (output.WriteBlock($"public {propertyType} {propertyName}"))
        {
            using (output.WriteBlock("get"))
            {
                output.WriteLine($"return _element.GetProperty(\"{propertyName}\").{conversionMethod}();");
            }
        }
    }
}
