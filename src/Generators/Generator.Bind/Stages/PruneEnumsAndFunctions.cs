using System;
using System.Collections.Generic;
using System.Linq;
using Bind.Generators;
using Bind.XML.Signatures;

namespace Bind.Stages
{
    /// <summary>
    ///     Prune enums and functions that are not used or have been removed.
    /// </summary>
    internal static class PruneEnumsAndFunctions
    {
        /// <summary>
        ///     Do the thing.
        /// </summary>
        public static GLXmlDefinitions Prune(IGeneratorSettings settings, GLXmlDefinitions data)
        {
            var newEnumValues = new Dictionary<string, GLXmlDefinitions.EnumValueDefinition>();
            var newFunctions = new Dictionary<string, GLXmlDefinitions.FunctionDefinition>();

            // Handles additions and removals at once,
            // pretty easy.
            foreach (var feature in data.Features.OrderBy(f => f.Version))
            {
                foreach (var addition in feature.Additions)
                {
                    switch (addition.Type)
                    {
                        case GLXmlDefinitions.FeatureElementType.Enum:
                            if (!newEnumValues.ContainsKey(addition.Name))
                            {
                                newEnumValues.Add(addition.Name, data.EnumValues[addition.Name]);
                            }

                            break;
                        case GLXmlDefinitions.FeatureElementType.Function:
                            if (!newFunctions.ContainsKey(addition.Name))
                            {
                                newFunctions.Add(addition.Name, data.Functions[addition.Name]);
                            }

                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                foreach (var removal in feature.Removals)
                {
                    switch (removal.Type)
                    {
                        case GLXmlDefinitions.FeatureElementType.Enum:
                            newEnumValues.Remove(removal.Name);
                            break;
                        case GLXmlDefinitions.FeatureElementType.Function:
                            newFunctions.Remove(removal.Name);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            return new GLXmlDefinitions(newEnumValues, data.Groups, newFunctions, data.Features);
        }
    }
}