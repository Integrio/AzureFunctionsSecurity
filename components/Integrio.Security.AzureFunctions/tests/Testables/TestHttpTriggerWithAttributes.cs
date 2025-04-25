using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Integrio.Security.AzureFunctions.Tests.Testables;

[FunctionAuthorize(AppRoles = ["Default", "Writer"], UserScopes = ["Api.Writer"])]
public class TestHttpTriggerWithAttributes
{
    [Function("TestHttpTrigger")]
    [FunctionAuthorize(AppRoles = ["Reader"], UserScopes = ["Api.Reader"])]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req,
        FunctionContext context)
    {
        return new OkObjectResult("Welcome to Azure Functions!");
    }
}

