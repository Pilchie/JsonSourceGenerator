using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using Generator.Extensions;

using Microsoft.CodeAnalysis;

namespace Generator;

public class SourceCodeBuilder
{
    private DiagnosticDescriptor UnableToGenerateName { get; } = new DiagnosticDescriptor(
        id: "NSG001",
        title: "Unable to generate property name.",
        messageFormat: "Unable to generate property name.",
        category: "SourceGenerator",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private readonly INamedTypeSymbol _attributeSymbol;
    private readonly INamedTypeSymbol _notifySymbol;
    private readonly INamedTypeSymbol _objectSymbol;
    private readonly Action<Diagnostic> _reportDiagnostic;

    public SourceCodeBuilder(
        INamedTypeSymbol attributeSymbol,
        INamedTypeSymbol notifySymbol,
        INamedTypeSymbol objectSymbol,
        Action<Diagnostic> reportDiagnostic)
    {
        _attributeSymbol = attributeSymbol ?? throw new ArgumentNullException(nameof(attributeSymbol));
        _notifySymbol = notifySymbol ?? throw new ArgumentNullException(nameof(notifySymbol));
        _objectSymbol = objectSymbol ?? throw new ArgumentNullException(nameof(objectSymbol));
        _reportDiagnostic = reportDiagnostic ?? throw new ArgumentNullException(nameof(reportDiagnostic));
    }


    public bool TryGeneratePartialType(
        INamedTypeSymbol type,
        ImmutableArray<IFieldSymbol> fields,
        [NotNullWhen(true)] out string? partialType)
    {
        using var codeTextWriter = new SourceCodeWriter();
        partialType = null;

        if (TryGeneratePartialType(codeTextWriter, type, fields))
        {
            partialType = codeTextWriter.ToString();
            return true;
        }

        return false;
    }

    private bool TryGeneratePartialType(
        SourceCodeWriter output,
        INamedTypeSymbol type,
        ImmutableArray<IFieldSymbol> fields)
    {
        using (output.WriteOutNamespace(type))
        {
            // Handle nested class
            var indentations = ImmutableArray<IDisposable>.Empty;
            if (!type.ContainingSymbol.Equals(type.ContainingNamespace, SymbolEqualityComparer.Default))
            {
                var containingTypes = type.GetContainingTypes().ToImmutableArray();
                var builder =  ImmutableArray.CreateBuilder<IDisposable>(containingTypes.Length);
                foreach (var containingType in containingTypes.Reverse())
                {
                    builder.Add(WriteOutPartialTypeDeclaration(output, containingType));
                }

                indentations = builder.MoveToImmutable();
            }

            // Write out type declaration
            using (WriteOutPartialTypeDeclaration(output, type, _notifySymbol))
            {
                // If the type doesn't inherit from a type that implements INotifyPropertyChanged already,
                // or inherits from a type that _will_ inherit from it after the source generator runs
                // implement the PropertyChanged event.
                if (!type.AllInterfaces.Any(t => t.Equals(_notifySymbol, SymbolEqualityComparer.Default)) &&
                    !type.AnyBaseTypesHaveFieldsWithAttribute(_attributeSymbol))
                {
                    output.WriteOutPropertyChangedImplementation();
                }

                // /create properties for each field 
                foreach (var fieldSymbol in fields)
                {
                    if (!TryWriteOutProperty(output, fieldSymbol, _attributeSymbol, location => _reportDiagnostic(Diagnostic.Create(UnableToGenerateName, location))))
                    {
                        return false;
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

    private IDisposable WriteOutPartialTypeDeclaration(
        SourceCodeWriter output,
        INamedTypeSymbol type,
        params INamedTypeSymbol[] additionalInterfaces)
    {
        var accessibility = type.DeclaredAccessibility switch
        {
            Accessibility.Public => "public",
            Accessibility.Internal => "internal",
            Accessibility.Protected => "protected",
            Accessibility.Private => "private",
            Accessibility.ProtectedAndInternal => "private protected",
            Accessibility.ProtectedOrInternal => "protected internal",
            _ => throw new InvalidOperationException()
        };
        var modifiers = GetModifiersString(type);
        var typeKind = GetTypeKindString(type);
        var typeName = type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        var inheritsList = GetInherits(type, _objectSymbol, additionalInterfaces);
        var implementsList = GetImplements(type, additionalInterfaces);

        return output.WriteOutPartialTypeDeclaration(accessibility, modifiers, typeKind, typeName, inheritsList, implementsList);

        static string GetModifiersString(INamedTypeSymbol type)
        {
            var modifiers = "partial";
            if (type.IsAbstract)
            {
                modifiers = "abstract " + modifiers;
            }
            if (type.IsSealed)
            {
                modifiers = "sealed " + modifiers;
            }
            if (type.IsStatic)
            {
                modifiers = "static " + modifiers;
            }
            return modifiers;
        }

        static string GetTypeKindString(INamedTypeSymbol type)
            => (type.IsRecord, type.TypeKind) switch
            {
                (true, TypeKind.Struct) => "record struct",
                (true, TypeKind.Class) => "record",
                (false, TypeKind.Class) => "class",
                (false, TypeKind.Enum) => "enum",
                (false, TypeKind.Interface) => "interface",
                (false, TypeKind.Struct) => "struct",
                _ => throw new NotImplementedException(),
            };

        static ImmutableArray<string> GetInherits(
            INamedTypeSymbol type,
            INamedTypeSymbol systemObject,
            INamedTypeSymbol[] additionalInterfaces)
        {
            var builder = ImmutableArray.CreateBuilder<string>();

            var baseType = type.BaseType;
            if (baseType is not null &&
                !baseType.Equals(systemObject, SymbolEqualityComparer.Default))
            {
                builder.Add(baseType.ToDisplayString());
            }

            if (type.TypeKind == TypeKind.Interface)
            {
                builder.AddRange(type.AllInterfaces.Concat(additionalInterfaces).Select(i => i.ToDisplayString()));
            }

            return builder.ToImmutable(); ;
        }

        static string[] GetImplements(
            INamedTypeSymbol type,
            INamedTypeSymbol[] additionalInterfaces)
        {
            return type.TypeKind != TypeKind.Interface
                ? type.AllInterfaces.Concat(additionalInterfaces).Select(i => i.ToDisplayString()).ToArray()
                : Array.Empty<string>();
        }
    }

    private static bool TryWriteOutProperty(SourceCodeWriter output, IFieldSymbol fieldSymbol, INamedTypeSymbol attributeSymbol, Action<Location?> reportDiagnostic)
    {
        // get the name and type of the field
        var fieldName = fieldSymbol.Name;
        var fieldType = fieldSymbol.Type.ToDisplayString();

        // get the AutoNotify attribute from the field, and any associated data
        var attributeData = fieldSymbol.GetAttributes().Single(ad => ad.AttributeClass?.Equals(attributeSymbol, SymbolEqualityComparer.Default) == true);
        var overriddenNameOpt = attributeData.NamedArguments.SingleOrDefault(kvp => kvp.Key == "PropertyName").Value;
        string propertyName = ChooseName(fieldName, overriddenNameOpt);
        if (propertyName.Length == 0 || propertyName == fieldName)
        {
            reportDiagnostic(fieldSymbol.Locations.FirstOrDefault());
            return false;
        }

        output.WriteOutProperty(fieldName, fieldType, propertyName);
        return true;

        static string ChooseName(string fieldName, TypedConstant overriddenNameOpt)
        {
            if (!overriddenNameOpt.IsNull)
            {
                return overriddenNameOpt.Value!.ToString() ?? string.Empty;
            }

            fieldName = fieldName.TrimStart('_');
            return fieldName.Length switch
            {
                0 => string.Empty,
                1 => fieldName.ToUpper(),
                _ => fieldName[..1].ToUpper() + fieldName[1..]
            };
        }
    }
}
