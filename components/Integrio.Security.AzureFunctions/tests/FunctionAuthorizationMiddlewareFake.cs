using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Integrio.Security.AzureFunctions.Tests;

public class FunctionAuthorizationMiddlewareFake(
    ILogger<FunctionAuthorizationMiddleware> logger,
    ITokenValidator tokenValidator,
    TokenValidationParameters tokenValidationParameters, 
    bool disableAuthentication) : FunctionAuthorizationMiddleware(logger, tokenValidator, tokenValidationParameters, disableAuthentication)
{
    public string? ResponseMessage { get; set; }

    protected override void SetResponse(FunctionContext context, HttpResponseData responseData)
    {
        ResponseMessage = responseData.Body.ToString() ?? string.Empty;
    }
}