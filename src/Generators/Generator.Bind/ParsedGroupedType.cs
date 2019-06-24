using JetBrains.Annotations;

namespace Bind
{
    internal struct ParsedGroupedType
    {
        public ParsedType Type;

        [CanBeNull] public string GroupName;

        public ParsedGroupedType(ParsedType type, [CanBeNull] string groupName)
        {
            Type = type;
            GroupName = groupName;
        }

        public void Deconstruct(out ParsedType type, [CanBeNull] out string groupName)
        {
            type = Type;
            groupName = GroupName;
        }
    }
}