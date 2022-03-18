using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;

namespace Generator.Extensions;
internal static class INamedTypeSymbolExtensions
{
    public static IEnumerable<INamedTypeSymbol> GetContainingTypes(this INamedTypeSymbol type)
    {
        var current = type.ContainingType;
        while (current != null)
        {
            yield return current;
            current = current.ContainingType;
        }
    }
}
