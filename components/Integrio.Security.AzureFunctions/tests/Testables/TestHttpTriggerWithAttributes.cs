using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace Integrio.Security.AzureFunctions.Tests.Testables;

[FunctionAuthorize("Default", "Writer")]
public class TestHttpTriggerWithAttributes
{
    [Function("TestHttpTrigger")]
    [FunctionAuthorize("Reader")]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req,
        FunctionContext context)
    {
        return new OkObjectResult("Welcome to Azure Functions!");
    }
}

