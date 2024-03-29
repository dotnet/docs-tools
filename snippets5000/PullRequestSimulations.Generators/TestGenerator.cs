﻿using Microsoft.CodeAnalysis;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace PullRequestSimulations.Generators
{
    [Generator]
    public class TestGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            // Find the main method
            var mainMethod = context.Compilation.GetEntryPoint(context.CancellationToken);

            // Build up the source code
            string sourceHeader =
$@"// <auto-generated/>
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;

namespace PullRequestSimulations;

public partial class LocalTests
{{
        
";

            string sourceFooter =
$@"
        
}}
";

            StringBuilder builder = new StringBuilder();
            builder.AppendLine(sourceHeader);

            var file = context.AdditionalFiles.Where(f => Path.GetFileName(f.Path).Equals("data.json", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();

            if (file != null)
            {
                int counter = 20;

                foreach (JObject item in JArray.Parse(File.ReadAllText(file.Path)))
                {
                    string name = item.GetValue("Name").ToString();
                    builder.AppendLine($"");
                    builder.AppendLine($"    [TestMethod(\"{name}\")]");
                    builder.AppendLine($"    public void Run{counter}() =>");
                    builder.AppendLine($"        RunTest(\"{name}\");");
                    counter++;
                }
            }

            builder.AppendLine(sourceFooter);

            context.AddSource($"LocalTests.g.cs", builder.ToString());
        }

        public void Initialize(GeneratorInitializationContext context)
        {
#if DEBUG
            if (!System.Diagnostics.Debugger.IsAttached)
            {
                //System.Diagnostics.Debugger.Launch();
            }
#endif
        }
    }
}
