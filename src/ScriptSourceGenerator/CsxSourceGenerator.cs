using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Scripting;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Versioning;

namespace ScriptSourceGenerator;

/// <summary>
/// Csx Source Generator
/// </summary>
[Generator]
public partial class CsxSourceGenerator : IIncrementalGenerator
{
    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // find all additional files that end with .txt
        var textFiles = context.AdditionalTextsProvider.Where(static file => file?.Path.EndsWith(".csx", StringComparison.InvariantCultureIgnoreCase) == true);

        // read their contents and save their name
        var namesAndContents = textFiles.Select(static (text, cancellationToken) => (name: Path.GetFileNameWithoutExtension(text.Path), content: text.GetText(cancellationToken)!.ToString()));

        // generate a class that contains their values as const strings
        context.RegisterSourceOutput(namesAndContents, static (spc, nameAndContent) =>
        {
            var dependencies = new ConcurrentDictionary<string, MetadataReference>();
            var resolver = new NugetPackageReferenceResolver(ScriptOptions.Default.MetadataResolver, dependencies);

            var syntaxTree = CSharpSyntaxTree.ParseText(nameAndContent.content);
            var directive = syntaxTree.GetRoot().GetFirstDirective();
            while (directive != null)
            {
                if (directive is ReferenceDirectiveTriviaSyntax referenceDirective)
                {
                    resolver.ResolveReference(referenceDirective.File.ValueText, null, new MetadataReferenceProperties());
                }
                directive = directive.GetNextDirective();
            }

            var script = CSharpScript.Create(nameAndContent.content,
                ScriptOptions.Default
                .WithLanguageVersion(LanguageVersion.Preview)
                .WithMetadataResolver(resolver)
                .WithReferences(dependencies.Values),
                typeof(Globals));            

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
