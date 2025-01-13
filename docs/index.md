# Integrio.Security.AzureFunctions

Integrio.Security.AzureFunctions is a library that provides authorization middleware for Azure Functions, allowing you to easily secure your HTTP-triggered functions with token validation and claims-based authorization. 

## Features

- Token validation using Azure AD
- Claims-based authorization
- Easy integration with Azure Functions
- Configurable through app settings or code
- Class and method level authorization attributes
- Support for multiple roles and policies


> **Note:** This library only supports JWT Bearer Tokens that are sent in the `authorization` header of the HTTP request.

## Installation

You can install the package via NuGet:

```sh
# Add source
dotnet nuget add source "https://gitlab.com/api/v4/projects/64410497/packages/nuget/index.json" -n "GitLabIntropyAzureFunctionsSecurity" -u <your username> -p <your gitlab pat>

# Or update source
dotnet nuget update source GitLabIntropyAzureFunctionsSecurity -s "https://gitlab.com/api/v4/projects/64410497/packages/nuget/index.json" -u <your username> -p <your gitlab pat>

# Install package
dotnet add package Integrio.Security.AzureFunctions
```

## Configure

To use the library in your Azure Functions project, you can configure it by adding `builder.UseFunctionAuthorization();` during you application initialization phase. You can use either built in support for configuration or code. See below examples:

### Using Configuration

```csharp
using Integrio.Security.AzureFunctions;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    //Depending on your framework use either ConfigureFunctionsWorkerDefaults or ConfigureFunctionsWebApplication
    //.ConfigureFunctionsWorkerDefaults(builder =>
    .ConfigureFunctionsWebApplication(builder =>
    {
        builder.UseFunctionAuthorization();
    })
    .ConfigureAppConfiguration((context, builder) =>
    {
        builder.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true);
    })
    .Build();

host.Run();
```
#### Example local.settings.json

```json
{
    "FunctionAuthorization": {
        "TenantId": "e3b0c442-98fc-4620-bb8d-5f5b6c3c4a7e",
        "ValidIssuers" : "https://sts.windows.net/e3b0c442-98fc-4620-bb8d-5f5b6c3c4a7e/, A comma separated string with valid issuers", 
        "ValidAudiences" : "api://MyTestApi, A comma separated string with valid audiences"
    }
}
```
### Alternative Configuration
You can also configure the authorization middleware programmatically:

```csharp
builder.UseFunctionAuthorization(
    tenantId: "e3b0c442-98fc-4620-bb8d-5f5b6c3c4a7e",
    validIssuers: new[] { "https://sts.windows.net/e3b0c442-98fc-4620-bb8d-5f5b6c3c4a7e/", "issuer2" },
    validAudiences: new[] { "api://MyTestApi", "audience2" }
);
```

## Usage
Basic Function with Authorization
Secure an HTTP-triggered function with role-based authorization with `FunctionAuthorize` applied both on class and method level.

> **NOTE:** In the below example the `FunctionAuthorize` library will allow function invocation if the JWT bearer token contains any of the role claims: `Default`, `Writer`, or `Reader`.

> **NOTE:** If no `FunctionAuthorize` attribute is present neither on class level nor on method level the function call will be authorized by default.
### Example Function 

```csharp
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Integrio.Security.AzureFunctions;

[FunctionAuthorize("Default", "Writer")]
public class TestHttpTrigger
{
    [Function("TestHttpTrigger")]
    [FunctionAuthorize("Reader")]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req, FunctionContext context)
    {
        return new OkObjectResult("Welcome to Azure Functions!");
    }
}
```
### Example: Accessing Claims
Access the authenticated user's claims:

```csharp
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Integrio.Security.AzureFunctions;

public class TestHttpTrigger
{
    [Function("UserInfo")]
    [FunctionAuthorize("User")]
    public IActionResult GetUserInfo(
        [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req,
        FunctionContext context)
    {
        var identity = context.GetClaimsIdentity();
        var claims = identity.Claims.Select(c => new 
        {
            Type = c.Type,
            Value = c.Value
        });
        
        return new OkObjectResult(claims);
    }
}
```
### Example: Disable Authentication
For development purposes, you can disable authentication in settings:

```json
{
  "Values": {
    "DisableAuthentication": true
  }
}
```
