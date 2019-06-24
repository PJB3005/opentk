using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Bind.XML;
using Bind.XML.Signatures;

namespace Bind.Stages
{
    /// <summary>
    ///     Parse function types.
    ///     Determines whether functions are in/out, pointer levels, etc...
    /// </summary>
    internal static class ParseFunctionTypes
    {
        private static readonly Regex ConstSpecifierRegex = new Regex(@"\wconst\w");

        public static IDictionary<string, ParsedFunctionDefinition> Parse(GLXmlDefinitions data)
        {
            return data.Functions.Values
                .ToDictionary(
                    f => f.Name,
                    f => new ParsedFunctionDefinition(
                        f.Name,
                        ParseType(f.ReturnType),
                        f.Parameters
                            .Select(ParseParameter)
                            .ToList()));
        }

        private static ParsedFunctionParameter ParseParameter(GLXmlDefinitions.FunctionParameter parameter)
        {
            var (groupedType, name) = parameter;

            var parsedType = ParseType(groupedType);
            ParsedFunctionParameter.FlowDirection flow;
            if (ConstSpecifierRegex.IsMatch(groupedType.TypeName))
            {
                flow = ParsedFunctionParameter.FlowDirection.Out;
            }
            else
            {
                flow = ParsedFunctionParameter.FlowDirection.In;
            }
            return new ParsedFunctionParameter(name, parsedType, flow);
        }

        public static ParsedGroupedType ParseType(GLXmlDefinitions.GroupedType groupedType)
        {
            var (type, groupName) = groupedType;

            return new ParsedGroupedType(ParseType(type), groupName);
        }

        public static ParsedType ParseType(string type)
        {
            // Use the existing type parser to parse the signature.
            // This is quite inefficient but oh well not like it matters.
            var signature = ParsingHelpers.ParseTypeSignature(type);

            // We treat it being an array as being another pointer level.
            // NOTE: The parser doesn't parse ArrayLevel correctly, so don't use that.
            // This will break if gl has a [][] type, but gl.xml has exactly ONE definition using an array
            // so I think we'll be fine.
            var pointerLevel = 0;
            if (signature.IsArray)
            {
                pointerLevel += 1;
            }

            // Add indirection level to the pointer level too.
            pointerLevel += signature.IndirectionLevel;

            return new ParsedType(signature.Name, pointerLevel);
        }
    }
}