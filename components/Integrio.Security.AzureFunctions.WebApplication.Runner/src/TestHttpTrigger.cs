using System.Security.Claims;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Integrio.Security.AzureFunctions.WebApplication.Runner;

[FunctionAuthorize("Default", "Writer")]
public class TestHttpTrigger(ILogger<TestHttpTrigger> logger, IClaimsIdentityProvider claimsIdentityProvider)
{
    [Function("TestHttpTrigger")]
    [FunctionAuthorize("Reader")]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req, FunctionContext context)
    {
        logger.LogInformation("C# HTTP trigger function processed a request.");

        if (claimsIdentityProvider.ClaimsIdentity is null)
        {
            logger.LogInformation("ClaimsIdentity not found!");
            return new UnauthorizedResult();
        }

        foreach (var claim in claimsIdentityProvider.ClaimsIdentity.Claims)
        {
            logger.LogInformation($"Claim Type: {claim.Type}, Claim Value: {claim.Value}");
        }
        
        return new OkObjectResult("Welcome to Azure Functions!");
    }

}