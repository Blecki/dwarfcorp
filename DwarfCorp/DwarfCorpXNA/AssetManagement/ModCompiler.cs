using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Reflection;

namespace DwarfCorp
{
    public static class ModCompiler
    {
        public static Assembly CompileCode(IEnumerable<String> Files, Action<String> ReportError)
        {
            var codeProvider = CodeDomProvider.CreateProvider("CSharp");

            var parameters = new CompilerParameters();
            parameters.GenerateInMemory = true;
            parameters.GenerateExecutable = false;

            parameters.ReferencedAssemblies.Add("mscorlib.dll");
            parameters.ReferencedAssemblies.Add("System.dll");
            parameters.ReferencedAssemblies.Add("System.Core.dll");
            parameters.ReferencedAssemblies.Add("System.Data.Linq.dll");
            //parameters.ReferencedAssemblies.Add("Microsoft.Xna.Framework.dll");
            //parameters.ReferencedAssemblies.Add("Microsoft.Xna.Framework.Game.dll");
            //parameters.ReferencedAssemblies.Add("Microsoft.Xna.Framework.Graphics.dll");
            parameters.ReferencedAssemblies.Add("Microsoft.Xna.Framework.Xact.dll");
            parameters.ReferencedAssemblies.Add("DwarfCorp.exe");
            parameters.ReferencedAssemblies.Add(Environment.GetEnvironmentVariable("XNAGSv4") + @"\References\Windows\x86\Microsoft.Xna.Framework.dll");
            parameters.ReferencedAssemblies.Add(Environment.GetEnvironmentVariable("XNAGSv4") + @"\References\Windows\x86\Microsoft.Xna.Framework.Game.dll");
            parameters.ReferencedAssemblies.Add(Environment.GetEnvironmentVariable("XNAGSv4") + @"\References\Windows\x86\Microsoft.Xna.Framework.Graphics.dll");

            var compilationResults = codeProvider.CompileAssemblyFromFile(parameters, Files.ToArray());

            bool realError = false;
            if (compilationResults.Errors.Count > 0)
            {
                foreach (var error in compilationResults.Errors)
                {
                    var cError = error as CompilerError;
                    if (!cError.IsWarning) realError = true;
                    ReportError(String.Format("{0} {1}: {2}", cError.FileName, cError.Line, cError.ErrorText));
                }
            }

            if (realError) return null;
            return compilationResults.CompiledAssembly;
        }
    }
}
