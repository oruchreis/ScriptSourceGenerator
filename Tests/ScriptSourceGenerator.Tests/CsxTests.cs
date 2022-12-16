using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ScriptSourceGenerator.Test;

[TestClass]
public class CsxTests : VerifyBase
{
    private Task VerifyCsx(params (string path, string content)[] files)
    {
        // Create an instance of our EnumGenerator incremental source generator
        var generator = new CsxSourceGenerator();

        // The GeneratorDriver is used to run our generator against a compilation
        var driver = CSharpGeneratorDriver.Create(generator)
            .AddAdditionalTexts(files.Select(f => new CsxAdditionalText(f.path, f.content)).Cast<AdditionalText>().Union(
                new[] {
                    new CsxAdditionalText(Path.Combine(TestContext.TestDeploymentDir,"NetStandard20Fixes.csx"),
                    File.ReadAllText(Path.Combine(TestContext.TestDeploymentDir,"NetStandard20Fixes.csx"))) }).ToImmutableArray())
            .RunGenerators(CSharpCompilation.Create("test"));
        // Use verify to snapshot test the source generator output!        
        return Verify(driver)
            .UseDirectory("Snapshots");
    }

    [TestMethod]
    public Task OutputTest()
    {
        return VerifyCsx(("OutputTest.csx", """"
Output["deneme.cs"].Append("test content");
Output["deneme2.cs"].Append("""
test content 2
""");
""""));
    }

    [TestMethod]
    public Task NugetTest()
    {
        return VerifyCsx(("NugetTest.csx", """"
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
            """"));
    }

    [TestMethod]
    public Task LoadTest()
    {
        return VerifyCsx((Path.Combine(TestContext.TestDeploymentDir, "NugetTest2.csx"), """"            
            #load "NetStandard20Fixes.csx"
            #r "nuget: NSwag.Core.Yaml/13.18.0"
            #r "nuget: NSwag.CodeGeneration.CSharp/13.18.0"            
            #r "System.Net.Http"
            using System.Net.Http;
            using NJsonSchema.Generation;
            using NJsonSchema.Yaml;
            using NSwag;
            using NSwag.CodeGeneration.CSharp;

            string yaml;
            using (var httpClient = new HttpClient())
                yaml = await httpClient.GetStringAsync("https://raw.githubusercontent.com/OAI/OpenAPI-Specification/master/examples/v3.0/petstore.yaml");

            var openApiDocument = await OpenApiYamlDocument.FromYamlAsync(yaml);

            var settings = new CSharpClientGeneratorSettings
            {    

            };

            var generator = new CSharpClientGenerator(openApiDocument, settings);    

            Output["g.cs"].Append(generator.GenerateFile());
            """"));
    }
}