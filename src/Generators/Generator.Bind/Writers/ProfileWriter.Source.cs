using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bind.Structure;
using Bind.XML.Documentation;
using Bind.XML.Signatures;
using Bind.XML.Signatures.Enumerations;
using Bind.XML.Signatures.Functions;
using Humanizer;
using JetBrains.Annotations;

namespace Bind.Writers
{
    internal partial class ProfileWriter
    {
        /// <summary>
        /// Asynchronously writes an enum to a file.
        /// </summary>
        /// <param name="enum">The enum to write.</param>
        /// <param name="file">The file to write to.</param>
        /// <param name="ns">The namespace of this enum.</param>
        /// <param name="prefix">The constant prefix for the profile.</param>
        /// <returns>The asynchronous task.</returns>
        public static async Task WriteEnumAsync(this EnumerationSignature @enum, string file, string ns, string prefix)
        {
            var sw = new StreamWriter(file);
            await sw.WriteLineAsync(EmbeddedResources.LicenseText);
            await sw.WriteLineAsync("using System;");
            await sw.WriteLineAsync();
            await sw.WriteLineAsync("namespace " + ns);
            await sw.WriteLineAsync("{");
            await sw.WriteLineAsync("    public enum " + @enum.Name);
            await sw.WriteLineAsync("    {");
            await WriteTokens(sw, @enum.Tokens, prefix);
            await sw.WriteLineAsync("    }");
            await sw.WriteLineAsync("}");
            await sw.FlushAsync();
            sw.Dispose();
        }

        private static async Task WriteTokens
        (
            [NotNull] StreamWriter sw,
            [NotNull] IEnumerable<TokenSignature> tokens,
            [NotNull] string prefix
        )
        {
            // Make sure everything is sorted. This will avoid random changes between
            // consecutive runs of the program.
            tokens = tokens.OrderBy(c => c.Value).ThenBy(c => c.Name).ToList();

            foreach (var token in tokens)
            {
                var valueString = $"0x{token.Value:X}";

                await sw.WriteLineAsync("        /// <summary>");
                var originalTokenName = $"{prefix}{token.Name.Underscore().ToUpperInvariant()}";
                await sw.WriteLineAsync($"        /// Original was {originalTokenName} = {valueString}");
                await sw.WriteLineAsync("        /// </summary>");

                var needsCasting = token.Value > int.MaxValue || token.Value < 0;
                if (needsCasting)
                {
                    Debug.WriteLine($"Warning: casting overflowing enum value \"{token.Name}\" from 64-bit to 32-bit.");
                    valueString = $"unchecked((int){valueString})";
                }

                if (token != tokens.Last())
                {
                    await sw.WriteLineAsync($"        {token.Name} = {valueString},");
                }
                else
                {
                    await sw.WriteLineAsync($"        {token.Name} = {valueString}");
                }
            }
        }

        /// <summary>
        /// Asynchronously writes this interface to a file.
        /// </summary>
        /// <param name="i">The interface.</param>
        /// <param name="file">The file to write to.</param>
        /// <param name="ns">This interface's namespace.</param>
        /// <param name="prefix">The function prefix for this interface.</param>
        /// <param name="doc">The profile's documentation.</param>
        /// <returns>The asynchronous task.</returns>
        public static async Task WriteInterfaceAsync(this Interface i, string file, string ns, string prefix, ProfileDocumentation doc)
        {
            var sw = new StreamWriter(file);
            await sw.WriteLineAsync(EmbeddedResources.LicenseText);
            await sw.WriteLineAsync("using AdvancedDLSupport;");
            await sw.WriteLineAsync("using OpenToolkit.Core.Native;");
            await sw.WriteLineAsync("using System;");
            await sw.WriteLineAsync("using System.Runtime.InteropServices;");
            await sw.WriteLineAsync("using System.Text;");
            await sw.WriteLineAsync();
            await sw.WriteLineAsync("namespace " + ns);
            await sw.WriteLineAsync("{");
            await sw.WriteLineAsync("    internal interface " + i.InterfaceName);
            await sw.WriteAsync("    {");
            foreach (var function in i.Functions)
            {
                await sw.WriteLineAsync();
                using (var sr = new StringReader(GetDocumentation(function, doc)))
                {
                    string line;
                    while ((line = await sr.ReadLineAsync()) != null)
                    {
                        await sw.WriteLineAsync("        " + line);
                    }
                }

                await sw.WriteLineAsync
                (
                    "        [NativeSymbol(\"" + prefix + function.NativeEntrypoint + "\")]"
                );
                await sw.WriteLineAsync
                (
                    $"        " +
                    $"[AutoGenerated(" +
                    $"Category = \"{function.Categories.First()}\", " +
                    $"Version = \"{function.IntroducedIn}\", " +
                    $"EntryPoint = \"{prefix}{function.NativeEntrypoint}\"" +
                    $")]"
                );
                using (var sr = new StringReader(GetDeclarationString(function) + ";"))
                {
                    string line;
                    while ((line = await sr.ReadLineAsync()) != null)
                    {
                        await sw.WriteLineAsync("        " + line);
                    }
                }
            }

            await sw.WriteLineAsync("    }");
            await sw.WriteLineAsync("}");
            await sw.FlushAsync();
            sw.Dispose();
        }

        private static async Task WriteProjectFileAsync(string ns, string dir, string subDir, string coreProj, bool ext)
        {
            if (File.Exists(Path.Combine(dir, ns + ".csproj")))
            {
                return;
            }

            var csproj = new StreamWriter(Path.Combine(dir, ns + ".csproj"));
            await csproj.WriteLineAsync("<Project Sdk=\"Microsoft.NET.Sdk\">");
            await csproj.WriteLineAsync();
            await csproj.WriteLineAsync("  <PropertyGroup>");
            await csproj.WriteLineAsync("    <TargetFramework>netstandard2.0</TargetFramework>");
            await csproj.WriteLineAsync("    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>");
            await csproj.WriteLineAsync("    <LangVersion>latest</LangVersion>");
            await csproj.WriteLineAsync("    <RootNamespace>" + ns + "</RootNamespace>");
            await csproj.WriteLineAsync("    <AssemblyName>" + ns + "</AssemblyName>");
            await csproj.WriteLineAsync("  </PropertyGroup>");
            await csproj.WriteLineAsync();
            await csproj.WriteLineAsync("  <ItemGroup>");
            if (ext)
            {
                await csproj.WriteLineAsync
                (
                    "    <ProjectReference Include=\"$(OpenTKSolutionRoot)\\src\\" +
                    subDir + "\\" + coreProj
                    + "\\" + coreProj + ".csproj\" />"
                );
            }
            else
            {
                await csproj.WriteLineAsync
                (
                    "    <ProjectReference Include=\"$(OpenTKSolutionRoot)\\src\\OpenTK.Core\\OpenTK.Core.csproj\" />"
                );
            }

            await csproj.WriteLineAsync("  </ItemGroup>");
            await csproj.WriteLineAsync();
            if (ext)
            {
                await csproj.WriteLineAsync("  <Import Project=\"..\\..\\..\\..\\props\\common.props\" />");
            }
            else
            {
                await csproj.WriteLineAsync("  <Import Project=\"..\\..\\..\\props\\common.props\" />");
            }

            await csproj.WriteLineAsync("  <Import Project=\"$(OpenTKSolutionRoot)\\props\\nuget-common.props\" />");
            await csproj.WriteLineAsync("  <Import Project=\"$(OpenTKSolutionRoot)\\props\\stylecop.props\" />");
            await csproj.WriteLineAsync("</Project>");
            await csproj.FlushAsync();
            csproj.Dispose();
        }
    }
}
