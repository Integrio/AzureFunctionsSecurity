using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Integrio.Security.AzureFunctions.Runner;

[FunctionAuthorize("Default", "Writer")]
public class TestHttpTrigger
{
    private readonly ILogger<TestHttpTrigger> _logger;

    public TestHttpTrigger(ILogger<TestHttpTrigger> logger)
    {
        _logger = logger;
    }

    [Function("TestHttpTrigger")]
    [FunctionAuthorize("Reader")]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        return new OkObjectResult("Welcome to Azure Functions!");
    }

}