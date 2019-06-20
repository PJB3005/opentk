using System.Collections.Generic;
using System.Linq;
using Bind.Extensions;
using Bind.XML.Signatures;

namespace Bind.Stages
{
    /// <summary>
    ///     Prunes groups and invalid enum values from the registry.
    /// </summary>
    internal static class PruneGroups
    {
        public static GLXmlDefinitions Prune(GLXmlDefinitions data)
        {
            var newGroups = data.Groups.ToDictionary(
                kv => kv.Key,
                kv => new GLXmlDefinitions.GroupDefinition(new HashSet<string>(kv.Value.Enums)));

            // Enum values have already been pruned at this point.
            // This removes any groups that do not have enum values.
            foreach (var (groupName, group) in data.Groups)
            {
                var newGroup = newGroups[groupName];
                foreach (var @enum in group.Enums)
                {
                    if (!data.EnumValues.ContainsKey(@enum))
                    {
                        newGroup.Enums.Remove(@enum);
                    }
                }

                if (newGroup.Enums.Count == 0)
                {
                    newGroups.Remove(groupName);
                }
            }

            return new GLXmlDefinitions(data.EnumValues, newGroups, data.Functions, data.Features);
        }
    }
}