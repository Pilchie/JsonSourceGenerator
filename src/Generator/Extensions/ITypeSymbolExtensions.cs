using System;
using System.Collections.Generic;

using Microsoft.CodeAnalysis;

namespace Generator.Extensions;
internal static class ITypeSymbolExtensions
{
    public static IEnumerable<INamedTypeSymbol> GetBaseTypes(this ITypeSymbol type, Func<INamedTypeSymbol, bool>? takeWhilePredicate = null)
    {
        var current = type.BaseType;
        while (current != null &&
            (takeWhilePredicate == null || takeWhilePredicate(current)))
        {
            yield return current;
            current = current.BaseType;
        }
    }

    public static IEnumerable<ITypeSymbol> GetBaseTypesAndThis(this ITypeSymbol type)
    {
        var current = type;
        while (current != null)
        {
            yield return current;
            current = current.BaseType;
        }
    }
}
