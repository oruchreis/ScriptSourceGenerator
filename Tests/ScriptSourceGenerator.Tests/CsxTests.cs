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
    public Task OutputTest()
    {
        return VerifyCsx("OutputTest.csx", """"
Output["deneme.cs"].Append("test content");
Output["deneme2.cs"].Append("""
test content 2
""");
"""");
    }

    [TestMethod]
    public Task NugetTest()
    {
        return VerifyCsx("NugetTest.csx", """"
            #r "nuget:Microsoft.OpenApi.Readers/1.4.5"
            #r "System.Net.Http"
            using System;
            using System.Net.Http;
            using Microsoft.OpenApi;
            using Microsoft.OpenApi.Extensions;
            using Microsoft.OpenApi.Readers;

            var httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://raw.githubusercontent.com/OAI/OpenAPI-Specification/")
            };

            var stream = await httpClient.GetStreamAsync("master/examples/v3.0/petstore.yaml");

            // Read V3 as YAML
            var openApiDocument = new OpenApiStreamReader().Read(stream, out var diagnostic);

            // Write V2 as JSON
            var outputString = openApiDocument.Serialize(OpenApiSpecVersion.OpenApi2_0, OpenApiFormat.Json);
            Output["GeneratedModels.cs"].Append(outputString);
            """");
    }
}