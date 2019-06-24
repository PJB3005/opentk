using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bind.Extensions;
using Bind.Generators;
using Bind.Structure;
using Bind.Writers;
using Bind.XML.Documentation;
using Bind.XML.Signatures;
using Bind.XML.Signatures.Enumerations;
using Bind.XML.Signatures.Functions;

namespace Bind.Stages
{
    /// <summary>
    ///     Writes the thing.
    /// </summary>
    internal static class Writer2
    {
        /// <summary>
        ///     Does the writing thing.
        /// </summary>
        /// <param name="settings">Settings.</param>
        /// <param name="data">Data.</param>
        public static void Write(IGeneratorSettings settings, GLXmlDefinitions data, IEnumerable<ParsedFunctionDefinition> functions)
        {
            var rootFolder = Path.Combine(Program.Arguments.OutputPath, settings.OutputSubfolder);
            var @namespace = settings.Namespace;
            var projectDir = Path.Combine(rootFolder, @namespace);
            if (!Directory.Exists(rootFolder))
            {
                Directory.CreateDirectory(rootFolder);
            }

            if (Directory.Exists(projectDir))
            {
                // Always delete the directory to ensure no garbage gets left behind.
                Directory.Delete(projectDir, true);
            }

            Directory.CreateDirectory(Path.Combine(rootFolder, @namespace));
            Directory.CreateDirectory(Path.Combine(rootFolder, ProfileWriter.ExtensionsFolder));

            // var interfaces = Path.Combine(projectDir, ProfileWriter.InterfacesFolder);
            var enums = Path.Combine(projectDir, ProfileWriter.EnumsFolder);

            Directory.CreateDirectory(projectDir);
            // Directory.CreateDirectory(interfaces);
            Directory.CreateDirectory(enums);

            NameContainerWriter.WriteNameContainer(
                Path.Combine(projectDir, $"{settings.APIIdentifier}LibraryNameContainer.cs"),
                @namespace,
                settings.APIIdentifier,
                settings.NameContainer);

            ProjectFileWriter.WriteProjectFile(
                @namespace,
                projectDir,
                settings.OutputSubfolder,
                @namespace,
                false);

            // Write a single "All" enum.
            {
                var path = Path.Combine(enums, "All.cs");
                var tokens = data.EnumValues.Select(c => new TokenSignature(c.Key, c.Value.Value)).ToList();
                var enumSignature = new EnumerationSignature("All", tokens);
                EnumWriter.WriteEnum(enumSignature, path, @namespace, settings.ConstantPrefix);
            }

            /*
            // Write enums.
            foreach (var (groupName, group) in data.Groups)
            {
                var path = Path.Combine(enums, groupName + ".cs");
                var tokens = group.Enums.Select(c => new TokenSignature(c, data.EnumValues[c].Value)).ToList();
                var enumSignature = new EnumerationSignature(groupName, tokens);
                EnumWriter.WriteEnum(enumSignature, path, @namespace, settings.ConstantPrefix);
            }
            */

            // Write interface.
            {
                var interfaceName = "I" + settings.ClassName;
                var path = Path.Combine(projectDir, interfaceName + ".cs");
                var functionSignatures = functions.Select(
                    f => new FunctionSignature(
                        f.Name,
                        f.Name,
                        new[] { "oof" },
                        "Core",
                        new Version(),
                        new TypeSignature(f.ReturnType.Type.TypeName, f.ReturnType.Type.PointerLevels, 0, false, false, false),
                        f.Parameters.Select(p => new ParameterSignature(
                            p.Name,
                            new TypeSignature(p.Type.Type.TypeName, p.Type.Type.PointerLevels, 0, false, false, false))).ToList()));

                var @interface = new Interface(interfaceName, functionSignatures);
                @interface.WriteInterface(path, @namespace, string.Empty, new ProfileDocumentation(Array.Empty<FunctionDocumentation>()), settings.Namespace);
            }
        }
    }
}