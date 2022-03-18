using System.Diagnostics.CodeAnalysis;
using System.Linq;

using Microsoft.CodeAnalysis;

namespace Generator.Extensions;
internal static class ISymbolExtensions
{
    /// <summary>
    /// Returns a value indicating whether the specified symbol has the specified
    /// attribute.
    /// </summary>
    /// <param name="symbol">
    /// The symbol being examined.
    /// </param>
    /// <param name="attribute">
    /// The attribute in question.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="symbol"/> has an attribute of type
    /// <paramref name="attribute"/>; otherwise <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// If <paramref name="symbol"/> is a type, this method does not find attributes
    /// on its base types.
    /// </remarks>
    public static bool HasAttribute(this ISymbol symbol, [NotNullWhen(returnValue: true)] INamedTypeSymbol? attribute)
    {
        return attribute != null && symbol.GetAttributes().Any(attr => attr.AttributeClass?.Equals(attribute, SymbolEqualityComparer.Default) == true);
    }
}
