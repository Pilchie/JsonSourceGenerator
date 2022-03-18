using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;

namespace Generator.Extensions;
internal static class INamedTypeSymbolExtensions
{
    public static bool AnyBaseTypesHaveFieldsWithAttribute(this INamedTypeSymbol namedType, INamedTypeSymbol attribute)
        => namedType.GetBaseTypes().Any(baseType => baseType.GetMembers().OfType<IFieldSymbol>().Any(field => field.HasAttribute(attribute)));

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
