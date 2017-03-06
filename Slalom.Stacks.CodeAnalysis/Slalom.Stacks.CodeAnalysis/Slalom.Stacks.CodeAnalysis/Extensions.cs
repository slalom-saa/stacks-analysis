using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Slalom.Stacks.CodeAnalysis
{
    public static class Extensions
    {
        public static IEnumerable<INamedTypeSymbol> AllBases(this INamedTypeSymbol instance)
        {
            if (instance == null)
            {
                yield break;
            }
            if (instance.BaseType != null)
            {
                yield return instance.BaseType;
            }
            if (instance.BaseType?.Name != "Object")
            {
                foreach (var parent in instance.BaseType.AllBases())
                {
                    yield return parent;
                }
            }
        }

        public static bool HasBase(this INamedTypeSymbol instance, string name)
        {
            return instance.AllBases().Any(e => e.Name == name);
        }

        public static bool IsMutable(this INamedTypeSymbol instance)
        {
            return instance.GetMembers().Any(e => (e as IPropertySymbol)?.SetMethod?.DeclaredAccessibility == Accessibility.Public);
        }

        public static bool HasFields(this INamedTypeSymbol instance)
        {
            return instance.GetMembers().OfType<IFieldSymbol>().Any(e => !e.Name.EndsWith("__BackingField"));
        }
    }
}