using System.Security.Claims;
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
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req, FunctionContext context)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");

        var claimsIdentity = context.GetClaimsIdentity();

        if (claimsIdentity is null)
        {
            return new UnauthorizedResult();
        }
        
        foreach (var claim in claimsIdentity.Claims)
        {
            _logger.LogInformation($"Claim Type: {claim.Type}, Claim Value: {claim.Value}");
        }
        
        return new OkObjectResult("Welcome to Azure Functions!");
    }

}