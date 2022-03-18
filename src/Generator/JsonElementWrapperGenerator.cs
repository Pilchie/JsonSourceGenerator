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
public partial class JsonElementWrapperGenerator : IIncrementalGenerator
{
    private const string NamespaceNameText = "JsonElementWrapper";
    private const string TypeNameText = "JsonElementWrapperAttribute";
    private const string AttributeFullTypeName = NamespaceNameText + "." + TypeNameText;
    private const string AttributeText = $@"
using System;
namespace {NamespaceNameText}
{{
    [AttributeUsage(AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
    [System.Diagnostics.Conditional(""JsonElementWrapper_DEBUG"")]
    sealed class {TypeNameText} : Attribute
    {{
        public {TypeNameText}()
        {{
        }}
    }}
}}
";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(static context => context.AddSource("JsonElementWrapper", AttributeText));

        // Get interfaces with our attributes grouped by containing type
        var interfaceSymbols = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: IsInterfaceDeclarationWithAttribute,
            transform: GetInterfaceSymbol)
        .Collect()
        .SelectMany(static (groups, _) => groups);

        var data = context.CompilationProvider.Select(
            static (compilation, token) =>
            {
                var jsonElement = compilation.GetTypeByMetadataName("System.Text.Json.JsonElement");
                var attribute = compilation.GetTypeByMetadataName(AttributeFullTypeName);
                var systemObject = compilation.GetSpecialType(SpecialType.System_Object);
                return (JsonElement: jsonElement, Attribute: attribute, Object: systemObject);
            })
            .Combine(interfaceSymbols.Collect());

        context.RegisterSourceOutput(data, GenerateSource);
    }

    private static bool IsInterfaceDeclarationWithAttribute(SyntaxNode node, CancellationToken _)
        => node is InterfaceDeclarationSyntax interfaceDeclarationSyntax && interfaceDeclarationSyntax.AttributeLists.Count > 0;

    private static INamedTypeSymbol? GetInterfaceSymbol(GeneratorSyntaxContext context, CancellationToken token)
    {
        var interfaceDeclarationSyntax = (InterfaceDeclarationSyntax)context.Node;
        // Get the symbol being declared by the field, and keep it if its annotated
        if (context.SemanticModel.GetDeclaredSymbol(interfaceDeclarationSyntax, cancellationToken: token) is INamedTypeSymbol namedTypeSymbol &&
            namedTypeSymbol.TypeKind == TypeKind.Interface &&
            namedTypeSymbol.GetAttributes().Any(ad => ad.AttributeClass?.ToDisplayString() == AttributeFullTypeName))
        {
            return namedTypeSymbol;
        }

        return null;
    }

    private static void GenerateSource(
        SourceProductionContext context,
        (
            (INamedTypeSymbol? JsonElement, INamedTypeSymbol? Attribute, INamedTypeSymbol? Object) RequiredSymbols,
            ImmutableArray<INamedTypeSymbol?> Interfaces
        ) data)
    {
        var attributeSymbol = data.RequiredSymbols.Attribute;
        var jsonElementSymbol = data.RequiredSymbols.JsonElement;
        var objectSymbol = data.RequiredSymbols.Object;
        if (attributeSymbol is null || jsonElementSymbol is null || objectSymbol is null)
        {
            return;
        }

        var builder = new SourceCodeBuilder(attributeSymbol, jsonElementSymbol, objectSymbol, context.ReportDiagnostic);
        foreach (var interfaceSymbol in data.Interfaces)
        {
            if (interfaceSymbol is not null && builder.TryGenerateSource(interfaceSymbol, out var content))
            {
                context.AddSource($"{interfaceSymbol.Name}_JsonElementWrapper.cs", SourceText.From(content, Encoding.UTF8));
            }
        }
    }
}
