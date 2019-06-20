using System.Collections.Generic;

namespace Bind
{
    internal struct ParsedFunctionDefinition
    {
        public string Name;
        public ParsedGroupedType ReturnType;
        public IList<ParsedFunctionParameter> Parameters;

        public ParsedFunctionDefinition(string name, ParsedGroupedType returnType, IList<ParsedFunctionParameter> parameters)
        {
            Name = name;
            ReturnType = returnType;
            Parameters = parameters;
        }
    }
}