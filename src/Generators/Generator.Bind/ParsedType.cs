namespace Bind
{
    internal struct ParsedType
    {
        public string TypeName;
        public int PointerLevels;

        public ParsedType(string typeName, int pointerLevels)
        {
            TypeName = typeName;
            PointerLevels = pointerLevels;
        }
    }
}