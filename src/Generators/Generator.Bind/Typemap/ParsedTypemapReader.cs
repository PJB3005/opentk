using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Bind.Stages;

namespace Bind.Typemap
{
    internal static class ParsedTypemapReader
    {
        public static IReadOnlyDictionary<string, ParsedType> Parse(TextReader reader)
        {
            var map = new Dictionary<string, ParsedType>();

            while (true)
            {
                var line = reader.ReadLine();
                if (line == null)
                {
                    break;
                }

                if (line.StartsWith("#") || string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var commentIndex = line.IndexOf("#", StringComparison.Ordinal);
                if (commentIndex > 0)
                {
                    line = line.Remove(commentIndex);
                }

                var parts = line.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2)
                {
                    throw new InvalidDataException("Typemap element with more or less than two columns found.");
                }

                var from = ParseFunctionTypes.ParseType(parts[0].Trim());
                if (from.PointerLevels != 0)
                {
                    continue;
                }
                var to = ParseFunctionTypes.ParseType(parts[1].Trim());

                map.Add(from.TypeName, to);
            }

            return map;
        }
    }
}