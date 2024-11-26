using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker.Http;

namespace Integrio.Security.AzureFunctions.WorkerDefaults.Runner;

public class TestHttpTrigger
{
    private readonly ILogger<TestHttpTrigger> _logger;

    public TestHttpTrigger(ILogger<TestHttpTrigger> logger)
    {
        _logger = logger;
    }

    [Function("TestHttpTrigger")]

    public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] 
        HttpRequestData req, 
        string employeeId, 
        FunctionContext context)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        
        var claimsIdentity = context.GetClaimsIdentity();
        if (claimsIdentity is null)
        {
            return await req.ToHttpResponse(HttpStatusCode.Unauthorized, "Invalid Bearer token");
        }
        
        foreach (var claim in claimsIdentity.Claims)
        {
            _logger.LogInformation($"Claim Type: {claim.Type}, Claim Value: {claim.Value}");
        }
        
        return await req.ToHttpResponse(HttpStatusCode.OK, "Weloce to Azure Functions!");
    }

}


public static class HttpRequestDataExtensions
{
    public static async Task<HttpResponseData> ToHttpResponse<T>(this HttpRequestData req, HttpStatusCode httpStatusCode, T payLoad, JsonSerializerOptions? jsonSerializerOptions = null)
    {
        var response = req.CreateResponse(httpStatusCode);
        response.Headers.Add("Content-Type", "application/json");
        var body = JsonSerializer.Serialize(payLoad, jsonSerializerOptions);
        await response.WriteStringAsync(body);
        return response;
    }
}