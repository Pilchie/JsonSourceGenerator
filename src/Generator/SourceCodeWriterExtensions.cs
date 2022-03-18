using System;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.CodeAnalysis;

namespace Generator.Extensions;
internal static class SourceCodeWriterExtensions
{
    public static IDisposable WriteOutNamespace(this SourceCodeWriter output, INamedTypeSymbol type)
    {
        return output.WriteBlock($"namespace {type.ContainingNamespace.ToDisplayString()}");
    }

    public static IDisposable WriteOutPartialTypeDeclaration(this SourceCodeWriter output,
        string accessibility,
        string modifiers,
        string typeKind,
        string typeName,
        ImmutableArray<string> inheritsList,
        string[] implementsList)
    {
        var inheritanceString = string.Join(", ", inheritsList.Concat(implementsList));

        if (!string.IsNullOrWhiteSpace(inheritanceString))
        {
            output.WriteLine($"{accessibility} {modifiers} {typeKind} {typeName} : {inheritanceString}");
        }
        else
        {
            output.WriteLine($"{accessibility} {modifiers} {typeKind} {typeName}");
        }
        return output.BeginWriteBlock();
    }

    public static void WriteOutPropertyChangedImplementation(this SourceCodeWriter output)
    {
        output.WriteLine("public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;");
        output.WriteLine("protected void NotifyPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = \"\")");
        using (output.Indent())
        {
            output.WriteLine("=> PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));");
        }
    }

    public static void WriteOutProperty(this SourceCodeWriter output, string fieldName, string fieldType, string propertyName)
    {
        output.WriteNewLine();
        using (output.WriteBlock($"public virtual {fieldType} {propertyName}"))
        {
            output.WriteLine($"get => this.{fieldName};");
            using (output.WriteBlock("set"))
            {
                using (output.WriteBlock($"if (!System.Collections.Generic.EqualityComparer<{fieldType}>.Default.Equals(this.{fieldName}, value))"))
                {
                    output.WriteLine($"this.{fieldName} = value;");
                    output.WriteLine($"NotifyPropertyChanged(nameof({propertyName}));");
                }
            }
        }
    }
}
