using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Bind.Extensions;
using Bind.Generators;
using JetBrains.Annotations;

namespace Bind.XML.Signatures
{
    /// <summary>
    ///     Parses the Khronos XML registry and filters it to a specific API.
    /// </summary>
    internal sealed class GLXmlReader
    {
        /// <summary>
        ///     This does not prune out unreferenced members from the API or anything.
        /// </summary>
        public static GLXmlDefinitions ParseXmlRegistry(
            IGeneratorSettings settings,
            [NotNull, PathReference] string signatureFilePath)
        {
            if (!File.Exists(signatureFilePath))
            {
                throw new FileNotFoundException("Couldn't find the given signatures file.", signatureFilePath);
            }

            XDocument document;
            using (var s = File.OpenRead(signatureFilePath))
            {
                document = XDocument.Load(s);
            }

            var root = document.Root;
            Debug.Assert(root != null, nameof(root) + " != null");

            var enumValues = ParseEnumValues(root);
            var groups = ParseGroups(root);
            var functions = ParseFunctions(root);
            var features = ParseFeatures(settings, root);

            return new GLXmlDefinitions(enumValues, groups, functions, features);
        }

        private static List<GLXmlDefinitions.FeatureDefinition> ParseFeatures(
            IGeneratorSettings settings,
            XElement root)
        {
            var features = new List<GLXmlDefinitions.FeatureDefinition>();

            foreach (var feature in root.Elements("feature"))
            {
                if (feature.GetRequiredAttribute("api").Value != settings.ApiName)
                {
                    continue;
                }

                var version = new Version(feature.GetRequiredAttribute("number").Value);
                var additions = ProcessFeatureList(settings, feature.Elements("require")).ToList();
                var removals = ProcessFeatureList(settings, feature.Elements("remove")).ToList();

                features.Add(new GLXmlDefinitions.FeatureDefinition(version, additions, removals));
            }

            return features;
        }

        private static IEnumerable<GLXmlDefinitions.FeatureElement> ProcessFeatureList(
            IGeneratorSettings settings,
            IEnumerable<XElement> elements)
        {
            foreach (var addition in elements
                .Where(e => ApiProfileMatch(settings, e))
                .Elements())
            {
                var name = addition.GetRequiredAttribute("name").Value;
                GLXmlDefinitions.FeatureElementType elementType;
                switch (addition.Name.LocalName)
                {
                    case "type":
                        continue;
                    case "enum":
                        elementType = GLXmlDefinitions.FeatureElementType.Enum;
                        name = name;
                        break;
                    case "command":
                        elementType = GLXmlDefinitions.FeatureElementType.Function;
                        name = name;
                        break;
                    default:
                        throw new InvalidDataException();
                }

                var additionElement = new GLXmlDefinitions.FeatureElement(name, elementType);
                yield return additionElement;
            }
        }

        private static bool ApiProfileMatch(IGeneratorSettings settings, XElement element)
        {
            var api = element.Attribute("api")?.Value;

            if (api != null && settings.ApiName != api)
            {
                return false;
            }

            var profile = element.Attribute("profile")?.Value;
            if (profile != null && settings.ApiProfile != profile)
            {
                return false;
            }

            return true;
        }

        private static Dictionary<string, GLXmlDefinitions.FunctionDefinition> ParseFunctions(XElement root)
        {
            var dictionary = new Dictionary<string, GLXmlDefinitions.FunctionDefinition>();
            foreach (var command in root.Elements("commands").Elements("command"))
            {
                var proto = command.GetRequiredElement("proto");
                var functionName = proto.GetRequiredElement("name").Value;

                var returnType = ParseType(proto);

                var parameters = new List<GLXmlDefinitions.FunctionParameter>();
                foreach (var param in command.Elements("param"))
                {
                    var paramType = ParseType(param);
                    var paramName = param.GetRequiredElement("name").Value;
                    parameters.Add(new GLXmlDefinitions.FunctionParameter(paramName, paramType));
                }

                var function = new GLXmlDefinitions.FunctionDefinition(functionName, returnType, parameters);
                dictionary.Add(functionName, function);
            }

            return dictionary;
        }

        private static Dictionary<string, GLXmlDefinitions.GroupDefinition> ParseGroups(XElement root)
        {
            var groups = new Dictionary<string, HashSet<string>>();

            foreach (var group in root.Elements("groups").Elements("group"))
            {
                var groupName = group.GetRequiredAttribute("name").Value;

                var set = new HashSet<string>();
                groups.Add(groupName, set);

                foreach (var groupMember in group.Elements())
                {
                    var value = groupMember.GetRequiredAttribute("name").Value;
                    set.Add(value);
                }
            }

            return groups.ToDictionary(
                kv => kv.Key,
                kv => new GLXmlDefinitions.GroupDefinition(kv.Value));
        }

        private static Dictionary<string, GLXmlDefinitions.EnumValueDefinition> ParseEnumValues(XElement root)
        {
            // Build a list of all available tokens.
            // Some tokens have a different value between GL and GLES,
            // so we need to keep separate lists for each API. Tokens
            // that are common go to the "default" list.
            var enumValues = new Dictionary<string, (long value, bool isApiSpecific)>();
            var enumerations = root.Elements("enums").Elements("enum");
            foreach (var e in enumerations)
            {
                var name = e.GetRequiredAttribute("name").Value;
                var api = e.Attribute("api")?.Value;

                // We already have an entry with the same name, but it's API specific.
                // We are not API specific, so that entry replaces us.
                if (enumValues.TryGetValue(name, out var tuple) && tuple.isApiSpecific && api == null)
                {
                    continue;
                }

                var value = e.GetRequiredAttribute("value").Value;
                long parsedValue;
                if (value.StartsWith("0x"))
                {
                    parsedValue = long.Parse(value.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                }
                else
                {
                    parsedValue = long.Parse(value, CultureInfo.InvariantCulture);
                }

                enumValues[name] = (parsedValue, api != null);
            }

            return enumValues.ToDictionary(
                kv => kv.Key,
                kv => new GLXmlDefinitions.EnumValueDefinition(kv.Value.value));
        }

        /*
        private static string TrimFunctionName(IGeneratorSettings settings, [NotNull] string name)
        {
            if (name.StartsWith(settings.FunctionPrefix))
            {
                return name.Remove(0, settings.FunctionPrefix.Length);
            }

            return name;
        }

        private static string TrimEnumName(IGeneratorSettings settings, [NotNull] string name)
        {
            if (name.StartsWith(settings.ConstantPrefix))
            {
                return name.Remove(0, settings.ConstantPrefix.Length);
            }

            return name;
        }
        */

        private static GLXmlDefinitions.GroupedType ParseType(XElement e)
        {
            // Parse the C-like <proto> element. Possible instances:
            // Return types:
            // - <proto>void <name>glGetSharpenTexFuncSGIS</name></proto>
            //   -> <returns>void</returns>
            // - <proto group="String">const <ptype>GLubyte</ptype> *<name>glGetString</name></proto>
            //   -> <returns>String</returns>
            // Parameter types:
            // - <param><ptype>GLenum</ptype> <name>shadertype</name></param>
            //   -> <param name="shadertype" type="GLenum" />
            // - <param len="1"><ptype>GLsizei</ptype> *<name>length</name></param>
            //   -> <param name="length" type="GLsizei" count="1" />
            var proto = e.Value;
            var name = e.GetRequiredElement("name").Value;
            var group = e.Attribute("group")?.Value;

            var ret = proto.Remove(proto.LastIndexOf(name, StringComparison.Ordinal)).Trim();

            return new GLXmlDefinitions.GroupedType(ret, group);
        }
    }
}