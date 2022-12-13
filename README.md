# ScriptSourceGenerator
[![Nuget](https://img.shields.io/nuget/v/ScriptSourceGenerator?color=1182c2&logo=nuget)](https://www.nuget.org/packages/ScriptSourceGenerator)

This geneator can compile and run `csx` files and generate files from csx scripts using Roslyn scripting.

## Usage
- Add nuget package into your project [![Nuget](https://img.shields.io/nuget/v/ScriptSourceGenerator?color=1182c2&logo=nuget)](https://www.nuget.org/packages/ScriptSourceGenerator)
- Add a `.csx` script file into project:
```csharp
var httpClient = new HttpClient();
var result = await client.GetStringAsync("http://www.github.com");
```
- If you want to create files as an output, you can use global `Output` indexer property which is a `StringBuilder`:
```csharp
Output["Github.html"].Append(result);
```
- Even you can create `.cs` files to generate compile time source codes:
```csharp
Output["MyModel.cs"].AppendLine("""
public class MyModel
{
""");
for(var i=0; i < 5; i++)
{
    Output["MyModel.cs"].AppendLine($$"""
        public string Prop{{i}} { get; set; } 
    """);
}

Output["MyModel.cs"].AppendLine("}");
```
and it will be generate this file:
```csharp
public class MyModel
{
    public string Prop0 { get; set; }
    public string Prop1 { get; set; }
    public string Prop2 { get; set; }
    public string Prop3 { get; set; }
    public string Prop4 { get; set; }
}
```
- To add assembly references and nuget references use `#r` directives:
```csharp
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
```