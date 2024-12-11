using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Scripting;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text;
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
            try
            {
                var name = Path.GetFileNameWithoutExtension(text.Path);
                var content = text.GetText(spc.CancellationToken)!.ToString();
                Debug.WriteLine($"source path: {text.Path}");
                var classFileDir = Path.GetDirectoryName(text.Path ?? "").Trim();
                if (string.IsNullOrEmpty(classFileDir))
                    classFileDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                Debug.WriteLine($"source dir: {classFileDir}");

                var dependencies = new ConcurrentDictionary<string, MetadataReference>();
                var resolver = new NugetPackageReferenceResolver(ScriptOptions.Default.MetadataResolver, dependencies);

                var syntaxTree = CSharpSyntaxTree.ParseText(content, CSharpParseOptions.Default
                    .WithLanguageVersion(LanguageVersion.Latest)
                    .WithKind(SourceCodeKind.Script),
                    text.Path ?? "", Encoding.UTF8);
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
                    .WithLanguageVersion(LanguageVersion.Latest)
                    .WithOptimizationLevel(OptimizationLevel.Debug)
                    .WithFilePath(text.Path)
                    .WithAllowUnsafe(true)
                    .WithEmitDebugInformation(true)
                    .WithFileEncoding(text.GetText()?.Encoding ?? Encoding.UTF8)
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
                }

                var globals = new Globals();
                try
                {
                    var result = script.RunAsync(globals: globals).Result;
                }
                catch (Exception e)
                {
                    spc.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("CSX0002", "Runtime Error", e.Message, "ScriptSourceGenerator", DiagnosticSeverity.Error, true, e.ToString(), e.HelpLink), null));
                }                

                foreach (var (fileName, outputContent) in globals.Output)
                {
                    spc.AddSource($"{name}.{fileName}", outputContent.ToString());
                }
            }
            catch (Exception e)
            {
                spc.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("CSX0001", "Internal Error", e.Message, "ScriptSourceGenerator", DiagnosticSeverity.Error, true, e.ToString(), e.HelpLink), null));
            }
        });
    }
}
