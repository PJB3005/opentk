namespace Bind
{
    internal struct ParsedFunctionParameter
    {
        public string Name;
        public ParsedGroupedType Type;
        public FlowDirection Flow;

        public ParsedFunctionParameter(string name, ParsedGroupedType type, FlowDirection flow)
        {
            Name = name;
            Type = type;
            Flow = flow;
        }

        public enum FlowDirection
        {
            In,
            Out
        }
    }
}