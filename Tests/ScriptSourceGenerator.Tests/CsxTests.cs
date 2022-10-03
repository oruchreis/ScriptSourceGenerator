using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace ScriptSourceGenerator.Test;

[TestClass]
public class CsxTests: VerifyBase
{
    private Task VerifyCsx(string path, string content)
    {
        // Create an instance of our EnumGenerator incremental source generator
        var generator = new CsxSourceGenerator();

        // The GeneratorDriver is used to run our generator against a compilation
        var driver = CSharpGeneratorDriver.Create(generator)
            .AddAdditionalTexts(ImmutableArray.CreateRange(new AdditionalText[] { new CsxAdditionalText(path, content) }))
            .RunGenerators(CSharpCompilation.Create("test"));
        // Use verify to snapshot test the source generator output!        
        return Verify(driver)
            .UseDirectory("Snapshots");
    }

    [TestMethod]
    public Task Test1()
    {
        return VerifyCsx("Test.csx", """"
Output["deneme.cs"].Append("test content");
Output["deneme2.cs"].Append("""
test content 2
""");
"""");
    }
}