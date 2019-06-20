using System;
using System.Collections.Generic;
using Bind.XML.Signatures.Functions;
using JetBrains.Annotations;

namespace Bind.XML.Signatures
{
    /// <summary>
    ///     Contains the parsed data from the Khronos XML registry,
    ///     for a specific API profile.
    /// </summary>
    internal class GLXmlDefinitions
    {
        public GLXmlDefinitions(
            IDictionary<string, EnumValueDefinition> enumValues,
            IDictionary<string, GroupDefinition> groups,
            IDictionary<string, FunctionDefinition> functions,
            IList<FeatureDefinition> features)
        {
            EnumValues = enumValues;
            Groups = groups;
            Functions = functions;
            Features = features;
        }

        public IDictionary<string, EnumValueDefinition> EnumValues;

        public IDictionary<string, GroupDefinition> Groups;

        public IDictionary<string, FunctionDefinition> Functions;

        public IList<FeatureDefinition> Features;

        public struct EnumValueDefinition
        {
            public EnumValueDefinition(long value)
            {
                Value = value;
            }

            public long Value;

            public override string ToString()
            {
                return $"Value: {Value}";
            }
        }

        public struct GroupDefinition
        {
            public GroupDefinition(ICollection<string> enums)
            {
                Enums = enums;
            }

            public ICollection<string> Enums;
        }

        public struct FunctionDefinition
        {
            public string Name;
            public GroupedType ReturnType;

            public IList<FunctionParameter> Parameters;

            public FunctionDefinition(string name, GroupedType returnType, IList<FunctionParameter> parameters)
            {
                Name = name;
                ReturnType = returnType;
                Parameters = parameters;
            }

            public override string ToString()
            {
                return $"{ReturnType} {Name}";
            }
        }

        public struct FunctionParameter
        {
            public GroupedType Type;
            public string Name;

            public FunctionParameter(string name, GroupedType type)
            {
                Name = name;
                Type = type;
            }

            public override string ToString()
            {
                return $"{Type} {Name}";
            }

            public void Deconstruct(out GroupedType type, out string name)
            {
                type = Type;
                name = Name;
            }
        }

        /// <summary>
        ///     Represents a type that may also be linked to a group by the XML registry.
        /// </summary>
        public struct GroupedType
        {
            [NotNull] public string TypeName;
            [CanBeNull] public string GroupName;

            public GroupedType([NotNull] string typeName, [CanBeNull] string groupName)
            {
                TypeName = typeName;
                GroupName = groupName;
            }

            public override string ToString()
            {
                if (GroupName != null)
                {
                    return $"({GroupName}) {TypeName}";
                }

                return TypeName;
            }

            public void Deconstruct([NotNull] out string typeName, [CanBeNull] out string groupName)
            {
                typeName = TypeName;
                groupName = GroupName;
            }
        }

        public struct FeatureDefinition
        {
            public Version Version;
            public IList<FeatureElement> Additions;
            public IList<FeatureElement> Removals;

            public FeatureDefinition(Version version, IList<FeatureElement> additions, IList<FeatureElement> removals)
            {
                Version = version;
                Additions = additions;
                Removals = removals;
            }

            public override string ToString()
            {
                return Version.ToString();
            }
        }

        public struct FeatureElement
        {
            public string Name;
            public FeatureElementType Type;

            public FeatureElement(string name, FeatureElementType type)
            {
                Name = name;
                Type = type;
            }

            public override string ToString()
            {
                return $"{Type} {Name}";
            }
        }

        public enum FeatureElementType
        {
            Enum,
            Function
        }
    }
}