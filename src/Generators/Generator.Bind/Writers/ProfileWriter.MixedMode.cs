using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Bind.Structure;

namespace Bind.Writers
{
    internal partial class ProfileWriter
    {
        public static async Task WriteMixedModeClassAsync(Project project, string name, string dir, string ns, bool ext)
        {
            await WriteOverloadsMixedModePartAsync(project, name, Path.Combine(dir, "GL.Overloads.cs"), ns);
            await WriteNativeMixedModePartAsync(project, name, Path.Combine(dir, "GL.Native.cs"), ns, ext);
            if (!File.Exists(Path.Combine(dir, "GL.cs")))
            {
                await WriteTemplateMixedModePartAsync(project, name, Path.Combine(dir, "GL.cs"), ns);
            }
        }
        
        private static async Task WriteTemplateMixedModePartAsync(Project project, string name, string file, string ns)
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
            await sw.WriteLineAsync("    public partial class " + name);
            await sw.WriteLineAsync("    {");
            foreach (var overload in project.Overloads)
            {
                await sw.WriteAsync("        public ");
                await sw.WriteLineAsync(GetDeclarationString(overload.Item1));
                await sw.WriteLineAsync("        {");
                foreach (var line in overload.ToString().Split(new[] {'\n'}, StringSplitOptions.RemoveEmptyEntries))
                {
                    await sw.WriteLineAsync("            " + line);
                }

                await sw.WriteLineAsync("        }");
                await sw.WriteLineAsync();
            }

            await sw.WriteLineAsync("    }");
            await sw.WriteLineAsync("}");
            sw.Dispose();
        }
        
        private static async Task WriteOverloadsMixedModePartAsync(Project project, string name, string file, string ns)
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
            await sw.WriteLineAsync("    public partial class " + name);
            await sw.WriteLineAsync("    {");
            foreach (var overload in project.Overloads)
            {
                await sw.WriteAsync("        public ");
                await sw.WriteLineAsync(GetDeclarationString(overload.Item1));
                await sw.WriteLineAsync("        {");
                foreach (var line in overload.ToString().Split(new[] {'\n'}, StringSplitOptions.RemoveEmptyEntries))
                {
                    await sw.WriteLineAsync("            " + line);
                }

                await sw.WriteLineAsync("        }");
                await sw.WriteLineAsync();
            }

            await sw.WriteLineAsync("    }");
            await sw.WriteLineAsync("}");
            sw.Dispose();
        }

        private static async Task WriteNativeMixedModePartAsync
        (
            Project project,
            string name,
            string file,
            string ns,
            bool ext
        )
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
            var @base = !ext ? "NativeLibraryBase" : "ExtensionBase";
            var nm = ns.Split('.').Last();
            await sw.WriteLineAsync("    public partial class " + name + " : " + @base + ", I" + name);
            await sw.WriteLineAsync("    {");
            await sw.WriteLineAsync("        /// <inheritdoc cref=\"" + @base + "\"/>");
            await sw.WriteLineAsync("        protected " + name + "(string path, ImplementationOptions options)");
            await sw.WriteLineAsync("            : base(path, options)");
            await sw.WriteLineAsync("        {");
            await sw.WriteLineAsync("        }");
            await sw.WriteLineAsync();
            await sw.WriteAsync("        public IPlatformLibraryNameContainer NameContainer => new");
            await sw.WriteLineAsync(" " + nm + "LibraryNameContainer();");
            await sw.WriteLineAsync("    }");
            await sw.WriteLineAsync("}");
            sw.Dispose();
        }
    }
}
