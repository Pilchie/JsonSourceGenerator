using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Generator;

[Generator(LanguageNames.CSharp)]
public partial class AutoNotifyGenerator : IIncrementalGenerator
{
    private const string NamespaceNameText = "AutoNotify";
    private const string TypeNameText = "AutoNotifyAttribute";
    private const string AttributeFullTypeName = NamespaceNameText + "." + TypeNameText;
    private const string AttributeText = $@"
using System;
namespace {NamespaceNameText}
{{
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    [System.Diagnostics.Conditional(""AutoNotifyGenerator_DEBUG"")]
    sealed class {TypeNameText} : Attribute
    {{
        public {TypeNameText}()
        {{
        }}
        public string PropertyName {{ get; set; }}
    }}
}}
";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(static context => context.AddSource("AutoNotifyAttribute", AttributeText));

        // Get fields with our attributes grouped by containing type
        var fieldSymbols = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: IsFieldDeclarationWithAttribute,
            transform: GetFieldSymbols)
        .SelectMany(static (fields, _) => fields)
        .Collect()
        .Select(GroupFieldsByContainingType)
        .SelectMany(static (groups, _) => groups);

        var data = context.CompilationProvider.Select(
            static (compilation, token) =>
            {
                var inotifyPropertyChanged = compilation.GetTypeByMetadataName("System.ComponentModel.INotifyPropertyChanged");
                var autoNotifyAttribute = compilation.GetTypeByMetadataName(AttributeFullTypeName);
                var systemObject = compilation.GetSpecialType(SpecialType.System_Object);
                return (INotifyPropertyChanged: inotifyPropertyChanged, AutoNotifyAttribute: autoNotifyAttribute, Object: systemObject);
            })
            .Combine(fieldSymbols.Collect());

        context.RegisterSourceOutput(data, GenerateSource);
    }

    private static bool IsFieldDeclarationWithAttribute(SyntaxNode node, CancellationToken _)
        => node is FieldDeclarationSyntax fieldDeclarationSyntax && fieldDeclarationSyntax.AttributeLists.Count > 0;

    private static IEnumerable<IFieldSymbol> GetFieldSymbols(GeneratorSyntaxContext context, CancellationToken token)
    {
        var fieldDeclarationSyntax = (FieldDeclarationSyntax)context.Node;
        foreach (var variable in fieldDeclarationSyntax.Declaration.Variables)
        {
            // Get the symbol being declared by the field, and keep it if its annotated
            if (context.SemanticModel.GetDeclaredSymbol(variable, cancellationToken: token) is IFieldSymbol fieldSymbol &&
                fieldSymbol.GetAttributes().Any(ad => ad.AttributeClass?.ToDisplayString() == AttributeFullTypeName))
            {
                yield return fieldSymbol;
            }
        }
    }

    private static IEnumerable<IGrouping<ISymbol?, IFieldSymbol>> GroupFieldsByContainingType(
        ImmutableArray<IFieldSymbol> fieldSymbols, CancellationToken _)
        => fieldSymbols.GroupBy(field => field.ContainingType, SymbolEqualityComparer.Default);

    private static void GenerateSource(
        SourceProductionContext context,
        (
            (INamedTypeSymbol? INotifyPropertyChanged, INamedTypeSymbol? AutoNotifyAttribute, INamedTypeSymbol Object) RequiredSymbols,
            ImmutableArray<IGrouping<ISymbol?, IFieldSymbol>> TypesAndFields
        ) data)
    {
        var attributeSymbol = data.RequiredSymbols.AutoNotifyAttribute;
        var notifySymbol = data.RequiredSymbols.INotifyPropertyChanged;
        var objectSymbol = data.RequiredSymbols.Object;
        if (attributeSymbol is null || notifySymbol is null)
        {
            return;
        }

        var builder = new SourceCodeBuilder(attributeSymbol, notifySymbol, objectSymbol, context.ReportDiagnostic);
        foreach (var group in data.TypesAndFields)
        {
            var containgingType = group.Key;
            if (containgingType is not INamedTypeSymbol namedTypeSymbol)
            {
                continue;
            }

            var fields = group.ToImmutableArray();
            if (builder.TryGeneratePartialType(namedTypeSymbol, fields, out var partialType))
            {
                context.AddSource($"{containgingType.Name}_autoNotify.cs", SourceText.From(partialType, Encoding.UTF8));
            }
        }
    }
}
