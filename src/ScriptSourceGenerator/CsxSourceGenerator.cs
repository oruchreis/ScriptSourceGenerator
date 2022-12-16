using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Scripting;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Versioning;
using System.Threading;

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

        // generate a class that contains their values as const strings
        context.RegisterSourceOutput(textFiles, static (spc, text) =>
        {
            var name = Path.GetFileNameWithoutExtension(text.Path);
            var content= text.GetText(spc.CancellationToken)!.ToString();
            Debug.WriteLine($"source path: {text.Path}");
            var classFileDir = Path.GetDirectoryName(text.Path ?? "").Trim();
            if (string.IsNullOrEmpty(classFileDir))
                classFileDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Debug.WriteLine($"source dir: {classFileDir}");

            var dependencies = new ConcurrentDictionary<string, MetadataReference>();
            var resolver = new NugetPackageReferenceResolver(ScriptOptions.Default.MetadataResolver, dependencies);

            var syntaxTree = CSharpSyntaxTree.ParseText(content);
            var imports = syntaxTree.GetCompilationUnitRoot().Usings.Select(u => u.Name.ToString()).ToList();
            var directive = syntaxTree.GetRoot().GetFirstDirective();
            while (directive != null)
            {
                if (directive is ReferenceDirectiveTriviaSyntax referenceDirective)
                {
                    resolver.ResolveReference(referenceDirective.File.ValueText, null, new MetadataReferenceProperties());
                }
                directive = directive.GetNextDirective();
            }

            var script = CSharpScript.Create(content,
                ScriptOptions.Default
                .WithLanguageVersion(LanguageVersion.Preview)
                .WithImports(imports)
                .WithMetadataResolver(resolver)
                .WithReferences(dependencies.Values)
                .WithSourceResolver(new SourceFileResolver(new string[] { }, classFileDir)),
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

            foreach (var (fileName, outputContent) in globals.Output)
            {
                spc.AddSource($"{name}.{fileName}", outputContent.ToString());
            }
        });
    }
}
