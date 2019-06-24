using System;
using System.Collections.Generic;
using System.Linq;

namespace Bind.Stages
{
    /// <summary>
    ///     Maps OpenGL function types to low-level C# equivalents.
    /// </summary>
    internal static class MapLowTypes
    {
        public static IEnumerable<ParsedFunctionDefinition> Map(
            IReadOnlyDictionary<string, ParsedType> typeMap,
            IEnumerable<ParsedFunctionDefinition> functions)
        {
            return functions.Select(
                f => new ParsedFunctionDefinition(
                    f.Name,
                    MapType(typeMap, f.ReturnType),
                    f.Parameters
                        .Select(p => new ParsedFunctionParameter(p.Name, MapType(typeMap, p.Type), p.Flow))
                        .ToList()));
        }

        public static ParsedGroupedType MapType(IReadOnlyDictionary<string, ParsedType> typeMap, ParsedGroupedType type)
        {
            var (typeName, group) = type;

            return new ParsedGroupedType(MapType(typeMap, typeName), group);
        }

        public static ParsedType MapType(IReadOnlyDictionary<string, ParsedType> typeMap, ParsedType type)
        {
            if (!typeMap.TryGetValue(type.TypeName, out var mapped))
            {
                Console.WriteLine($"Unable to map type {type.TypeName} to low-level C# type!");
                return type;
            }

            return new ParsedType(mapped.TypeName, mapped.PointerLevels + type.PointerLevels);
        }
    }
}