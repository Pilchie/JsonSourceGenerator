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

        context.RegisterSourceOutput(interfaceSymbols.Collect(), GenerateSource);
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
        ImmutableArray<INamedTypeSymbol?> interfaces)
    {
        var builder = new SourceCodeBuilder(context.ReportDiagnostic);
        foreach (var interfaceSymbol in interfaces)
        {
            if (interfaceSymbol is not null && builder.TryGenerateSource(interfaceSymbol, out var content))
            {
                context.AddSource($"{interfaceSymbol.Name}_JsonElementWrapper.cs", SourceText.From(content, Encoding.UTF8));
            }
        }
    }
}
