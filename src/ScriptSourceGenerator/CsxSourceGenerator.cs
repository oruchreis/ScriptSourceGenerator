using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace ScriptSourceGenerator;

public class Globals
{
    public OutputFiles Output { get; } = new OutputFiles();
}

[Generator]
public class CsxSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // find all additional files that end with .txt
        var textFiles = context.AdditionalTextsProvider.Where(static file => file?.Path.EndsWith(".csx") == true);

        // read their contents and save their name
        var namesAndContents = textFiles.Select(static (text, cancellationToken) => (name: Path.GetFileNameWithoutExtension(text.Path), content: text.GetText(cancellationToken)!.ToString()));

        // generate a class that contains their values as const strings
        context.RegisterSourceOutput(namesAndContents, (spc, nameAndContent) =>
        {
            var script = CSharpScript.Create(nameAndContent.content, ScriptOptions.Default.WithLanguageVersion(LanguageVersion.Preview), typeof(Globals));
            var diagnostics = script.Compile();
            if (diagnostics.Length > 0)
            {
                var hasError = false;
                foreach (var diagnostic in diagnostics)
                {
                    spc.ReportDiagnostic(diagnostic);
                    hasError = hasError || diagnostic.Severity == DiagnosticSeverity.Error;
                }

                if (hasError)
                    return;
            }

            var globals = new Globals();
            var result = script.RunAsync(globals: globals).Result;

            foreach (var (fileName, content) in globals.Output)
            {
                spc.AddSource($"{nameAndContent.name}.{fileName}", content.ToString());
            }            
        });
    }
}
