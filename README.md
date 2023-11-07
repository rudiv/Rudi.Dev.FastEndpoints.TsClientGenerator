# Rudi.Dev.FastEndpoints.TsClientGenerator

A library that generates simple TypeScript clients for FastEndpoints projects.

## Why

- TypeScript clients are needed
- Having to use Swagger is horrible
- The TS clients that NSwag generates are horrible
- TypeGen is cool
- Fluid is cool
- This is cool?

## Will it work with any FE project

No. This is relatively opinionated and expects the following:

- You name your endpoints correctly (ie. DoThisAndThatEndpoint)
- You name your requests correctly (ie. DoThisAndThatRequest)
- You name your responses correctly (ie. DoThisAndThatResponse)
- If you have a date on your Request/Response, it should be named 'DateXXXXX' (ie. DateCreated, DateOfBirth, DateLastModified) to be mapped to a real Date

## Usage

Add `Rudi.Dev.FastEndpoints.TsClientGenerator` from NuGet.

After `.UseFastEndpoints(..)`, add the following:
```csharp
await app.UseTsClientGenerator(o =>
{
    o.ApiClientOptions.BaseUrl = "https://localhost:7777";
    o.ApiClientOptions.ClassName = "MyApi";
    o.ApiClientOptions.Extends = "MyApiBase";
    o.ApiClientOptions.ExtendsImportPath = "$lib/api/MyApiBase";
    o.ApiClientOptions.ExtendsRequestInitMethod = "addHeaders";
    o.ApiClientOptions.ExportInstance = true;
})
    .GenerateAndWriteToFile("/path/to/apiclient.ts");
```

If you want to change the templates for a method, check them out first in /Templates. Then, you override with:

```csharp
o.TemplateOverrides.AddOverride(TemplateType.ClassBase, myTemplate);
```

## What's supported

It's important to note that, as it stands, this was written for our own development purposes. It doesn't support everything.

That said, I'd love for it to be able to. If there's any requests, or it doesn't work in your project - submit an issue
and repro (or suggested API). Or if you're that way inclined, a PR! 🥳

This is what's here (✅ or partially 😳), what's planned but not here yet (❌), and what probably should be here but just no (🤮):
- ✅ Sensible defaults for DTO generation (incl. TimeOnly (string)/DateOnly (Date) support, and [JsonPropertyName("whatever")] reading)
- ✅ GET Requests
- ✅ POST Requests
- ❌ PUT Requests
- ❌ DELETE Requests
- 🤮 PATCH Requests
- ✅ Export an instance of the client (ApiClientOptions.ExportInstance = true)
- ✅ Honour TypeGen attributes in project
- ✅ Ability to generate other TS objects from your code (use .GenerateAndWriteToFile(.., additionalTypeGenAssemblies: new [] { typeof(MyType).Assembly }) for this)
- ✅ Ability to override templates (see above)
- ✅ Filter the endpoints you're generating using endpoint tags, so you can generate multiple clients (use .GenerateAndWriteToFile(.., tags: new [] { "MyTag" }) for this
- ✅ Multiple Routes & Verbs per endpoint (within reason)
- ✅ Query Parameters Generation (when a Request object has a property that's not in the route)
- ✅ Path Parameters Generation (when a Request object has a property that's in the route)
- ✅ RequestInit transformer (ApiClientOptions.ExtendsRequestInitMethod) for transforming headers etc
- ❌ Request Body transformer for transforming the body
- ❌ Response transformer for transforming the response
- ✅ Date properties on Request/Response objects are mapped to Date objects
- 🤮 Multiple endpoints with the same name
- 😳 Multiple Request/Response DTOs with the same name. BUT DO NOT DO THIS. PLEASE. JUST NAME THEM CORRECTLY. This library attempts to resolve it by prefixing with the namespace, but if you're doing this wrong it's likely your endpoints are also wrong, so ^
- ❌ Generics on DTOs
- ❌ Azure Authorisation (via DefaultAzureCredential) - we need this, it will be here soon
- ❌ Override the generators for the DTOs/Client (ie. if you want to use a different library to TypeGen/Fluid)
