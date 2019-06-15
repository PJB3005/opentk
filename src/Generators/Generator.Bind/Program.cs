//
// Program.cs
//
// Copyright (C) 2019 OpenTK
//
// This software may be modified and distributed under the terms
// of the MIT license. See the LICENSE file for details.
//

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Threading.Tasks;
using Bind.Baking;
using Bind.Generators;
using Bind.Generators.ES;
using Bind.Generators.GL.Compatibility;
using Bind.Generators.GL.Core;
using Bind.Stages;
using Bind.Translation.Mappers;
using Bind.Typemap;
using Bind.Writers;
using Bind.XML.Documentation;
using Bind.XML.Overrides;
using Bind.XML.Signatures;
using Bind.XML.Signatures.Functions;
using CommandLine;
using JetBrains.Annotations;

namespace Bind
{
    /// <summary>
    /// Main class for the program.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// Gets the command-line arguments that were passed to the program.
        /// </summary>
        internal static CommandLineArguments Arguments { get; private set; }

        /// <summary>
        /// Gets a dictionary of cached profiles that have been read from signature files.
        /// </summary>
        private static readonly ConcurrentDictionary<string, IReadOnlyList<ApiProfile>> _cachedProfiles =
            new ConcurrentDictionary<string, IReadOnlyList<ApiProfile>>();

        /// <summary>
        /// Gets a dictionary of cached typemaps that have been read from file.
        /// </summary>
        private static readonly ConcurrentDictionary<string, IReadOnlyDictionary<TypeSignature, TypeSignature>> _cachedTypemaps =
            new ConcurrentDictionary<string, IReadOnlyDictionary<TypeSignature, TypeSignature>>();

        /// <summary>
        /// The main entry point of the application.
        /// </summary>
        /// <param name="args">A set of command-line arguments and switches to be parsed.</param>
        /// <returns>An integer, indicating success or failure. On a failure, a nonzero value is returned.</returns>
        private static async Task<int> Main(string[] args)
        {
            // force the GC to a suitable mode.
            GCSettings.LatencyMode = GCLatencyMode.Batch;
            Console.WriteLine($"OpenGL binding generator {Assembly.GetExecutingAssembly().GetName().Version} for OpenTK.");
            Console.WriteLine("For comments, bugs and suggestions visit http://github.com/opentk/opentk");
            Console.WriteLine();

            Parser.Default.ParseArguments<CommandLineArguments>(args)
                .WithParsed(r => Arguments = r);

            if (Arguments is null)
            {
                return 1;
            }

            var generators = CreateGenerators();

            var stopwatch = Stopwatch.StartNew();
            //await Task.WhenAll(generators.Select(p => Task.Run(() => GenerateBindings(p))));
            generators.ForEach(GenerateBindings);
            stopwatch.Stop();

            Console.WriteLine();
            Console.WriteLine("Bindings generated in {0} seconds.", stopwatch.Elapsed.TotalSeconds);

            return 0;
        }

        /// <summary>
        /// Asynchronously generates bindings for the API described by the given <see cref="IGeneratorSettings"/>
        /// object.
        ///
        /// Broadly, this takes the following steps:
        /// 1) Load the base API.
        /// 2) Bake overrides into the API
        /// 3) Bake Documentation into the API
        /// 4) Create mappings between OpenGL types and C# types
        /// 5) Apply the mappings to the API
        /// 6) Bake convenience overloads into the API (adding unsafe, etc)
        /// 7) Write the bindings to the files.
        ///
        /// </summary>
        /// <param name="generatorSettings">The settings describing the API.</param>
        private static void GenerateBindings([NotNull] IGeneratorSettings generatorSettings)
        {
            var khrSignaturePath = Path.Combine(Arguments.InputPath, "OpenGL", "gl.xml");

            // Step one:
            // Parse data for Khronos' XML registry.
            // This makes it easy to work on.
            var data = GLXmlReader.ParseXmlRegistry(generatorSettings, khrSignaturePath);

            // Step two:
            // Prune constants/functions so only ones required by the highest version still exist.
            data = PruneEnumsAndFunctions.Prune(generatorSettings, data);

            // Step three:
            // Prune groups and their contents.
            data = PruneGroups.Prune(generatorSettings, data);

            // Step ???:
            // Write the data to disk as C#.
            Writer2.Write(generatorSettings, data);

            /*
            var profileOverrides = OverrideReader
                .GetProfileOverrides(generatorSettings.OverrideFiles.ToArray())
                .ToList();

            var baker = new ProfileBaker(new[] { profile }, profileOverrides);
            var bakedProfile = baker.BakeProfile(
                generatorSettings.ApiNameShort,
                generatorSettings.Versions);

            var documentationPath = Path.Combine(
                Arguments.DocumentationPath,
                generatorSettings.SpecificationDocumentationPath);

            var doc = DocumentationReader.ReadProfileDocumentation(documentationPath, generatorSettings.FunctionPrefix);
            var bakedDocs = new DocumentationBaker(bakedProfile).BakeDocumentation(doc);


            var languageTypemapPath = Path.Combine(Arguments.InputPath, generatorSettings.LanguageTypemap);
            if (!_cachedTypemaps.TryGetValue(languageTypemapPath, out var languageTypemap))
            {
                using (var fs = File.OpenRead(languageTypemapPath))
                {
                    languageTypemap = new TypemapReader().ReadTypemap(fs);
                    _cachedTypemaps.TryAdd(languageTypemapPath, languageTypemap);
                }
            }

            var apiTypemapPath = Path.Combine(Arguments.InputPath, generatorSettings.APITypemap);
            if (!_cachedTypemaps.TryGetValue(apiTypemapPath, out var apiTypemap))
            {
                using (var fs = File.OpenRead(apiTypemapPath))
                {
                    apiTypemap = new TypemapReader().ReadTypemap(fs);
                    _cachedTypemaps.TryAdd(apiTypemapPath, apiTypemap);
                }
            }

            var bakedMap = TypemapBaker.BakeTypemaps(apiTypemap, languageTypemap);

            var mapper = new ProfileMapper(bakedMap);
            var mappedProfile = mapper.Map(bakedProfile);

            // var bindingsWriter = new BindingWriter(generatorSettings, overloadedProfile, bakedDocs);
            // await bindingsWriter.WriteBindingsAsync();
            ProfileWriter.Write(
                generatorSettings,
                mappedProfile,
                bakedDocs,
                generatorSettings.NameContainer);
            */
        }

        /// <summary>
        /// Populates the <see cref="Generators"/> field with the generators relevant for the current run.
        /// </summary>
        private static List<IGeneratorSettings> CreateGenerators()
        {
            var list = new List<IGeneratorSettings>();
            if (Arguments.TargetAPIs.Contains(TargetAPI.All))
            {
                list.Add(new DesktopCompatibilitySettings());
                list.Add(new DesktopSettings());
                list.Add(new EmbeddedSettings());
            }
            else
            {
                foreach (var targetAPI in Arguments.TargetAPIs)
                {
                    switch (targetAPI)
                    {
                        case TargetAPI.DesktopCompatibility:
                            list.Add(new DesktopCompatibilitySettings());
                            break;
                        case TargetAPI.Desktop:
                            list.Add(new DesktopSettings());
                            break;
                        case TargetAPI.Embedded:
                            list.Add(new EmbeddedSettings());
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            return list;
        }
    }
}
