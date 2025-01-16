using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker.Http;

namespace Integrio.Security.AzureFunctions.WorkerDefaults.Runner;

[FunctionAuthorize("Default", "Writer")]
public class TestHttpTrigger(ILogger<TestHttpTrigger> logger, IClaimsIdentityProvider claimsIdentityProvider)
{
    [Function("TestHttpTrigger")]
    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req,
        string employeeId,
        FunctionContext context)
    {
        logger.LogInformation("C# HTTP trigger function processed a request.");

        if (claimsIdentityProvider.ClaimsIdentity is null)
        {
            logger.LogInformation("ClaimsIdentity not found!");
            return await req.ToHttpResponse(HttpStatusCode.Unauthorized, "Unauthorized");
        }

        foreach (var claim in claimsIdentityProvider.ClaimsIdentity.Claims)
        {
            logger.LogInformation($"Claim Type: {claim.Type}, Claim Value: {claim.Value}");
        }

        return await req.ToHttpResponse(HttpStatusCode.OK, "Welcome to Azure Functions!");
    }
}

public static class HttpRequestDataExtensions
{
    public static async Task<HttpResponseData> ToHttpResponse<T>(this HttpRequestData req,
        HttpStatusCode httpStatusCode, T payLoad, JsonSerializerOptions? jsonSerializerOptions = null)
    {
        var response = req.CreateResponse(httpStatusCode);
        response.Headers.Add("Content-Type", "application/json");
        var body = JsonSerializer.Serialize(payLoad, jsonSerializerOptions);
        await response.WriteStringAsync(body);
        return response;
    }
}