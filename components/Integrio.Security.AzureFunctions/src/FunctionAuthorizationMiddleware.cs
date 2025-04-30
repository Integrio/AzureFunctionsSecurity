using System.Collections.Concurrent;
using System.Net;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Integrio.Security.AzureFunctions;

public class FunctionAuthorizationMiddleware(
    ILogger<FunctionAuthorizationMiddleware> logger, 
    ITokenValidator tokenValidator,
    TokenValidationParameters tokenValidationParameters,
    bool disableAuthentication) : IFunctionsWorkerMiddleware
{
    private readonly ConcurrentDictionary<string, List<string>> _requiredAppRolesCache = new();
    private readonly ConcurrentDictionary<string, List<string>> _requiredUserScopesCache = new();

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var requestData = await context.GetHttpRequestDataAsync();
        if (requestData is null) 
        {
            //Non HTTP triggered function call
            await next(context);
            return;
        }
        
        if (disableAuthentication)
        {
            logger.LogWarning("Authentication is disabled via configuration!");
            await next(context);
            return;
        }
        
        if (!TryGetTokenFromHeaders(requestData, out var token))
        {
            var responseData = await GetResponseAsync(context, HttpStatusCode.Unauthorized, "Missing Bearer token");
            SetResponse(context, responseData);
            return;
        }
        
        var tokenValidationResult = await tokenValidator.ValidateTokenAsync(token, tokenValidationParameters);
        if (!tokenValidationResult.IsValid)
        {
            var responseData = await GetResponseAsync(context, HttpStatusCode.Unauthorized, "Invalid Bearer token");
            SetResponse(context, responseData);
            return;
        }
        
        if (!Authorize(context, tokenValidationResult))
        {
            var responseData = await GetResponseAsync(context, HttpStatusCode.Forbidden, "Unauthorized");
            SetResponse(context, responseData);
            return;
        }
        
        var claimsIdentityProvider = context.InstanceServices.GetRequiredService<IClaimsIdentityProvider>();
        claimsIdentityProvider.ClaimsIdentity = tokenValidationResult.ClaimsIdentity;
        
        await next(context);
    }

    protected virtual void SetResponse(FunctionContext context, HttpResponseData responseData)
    {
        context.GetInvocationResult().Value = responseData;
    }

    private bool Authorize(FunctionContext context, TokenValidationResult tokenValidationResult)
    {
        var requiredAppRoles = _requiredAppRolesCache.GetOrAdd(context.FunctionDefinition.EntryPoint, _ =>
        {
            var targetMethod = GetTargetFunctionMethod(context);
            return GetRequiredAppRoles(targetMethod);
        });

        var requiredUserScopes = _requiredUserScopesCache.GetOrAdd(context.FunctionDefinition.EntryPoint, _ =>
        {
            var targetMethod = GetTargetFunctionMethod(context);
            return GetRequiredUserScopes(targetMethod);
        });
        
        if (requiredAppRoles.Count == 0 && requiredUserScopes.Count == 0)
        {
            //If no app roles or user scopes are defined on class or function method level, allow access
            return true;
        }
        
        var appRoles = tokenValidationResult.ClaimsIdentity.FindAll("roles").Select(x => x.Value);
        var userScopes = tokenValidationResult.ClaimsIdentity.FindFirst("scp")?.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries).AsEnumerable() ?? [];
        
        return appRoles.Any(role => requiredAppRoles.Contains(role)) || 
               userScopes.Any(scope => requiredUserScopes.Contains(scope));
    }
    
    private MethodInfo? GetTargetFunctionMethod(FunctionContext context)
    {
        var entryPoint = context.FunctionDefinition.EntryPoint;
        var assemblyPath = context.FunctionDefinition.PathToAssembly;
        var assembly = Assembly.LoadFrom(assemblyPath);
        var typeName = entryPoint[..entryPoint.LastIndexOf('.')];
        var type = assembly.GetType(typeName);
        var methodName = entryPoint[(entryPoint.LastIndexOf('.') + 1)..];
        return type?.GetMethod(methodName);
    }

    private List<string> GetRequiredAppRoles(MethodInfo? targetMethod)
    {
        var attributes = GetCustomAttributesOnClassAndMethod<FunctionAuthorizeAttribute>(targetMethod);
        // Only allow app roles that are common in class and method level attributes.
        return attributes
            .SelectMany(a => a.AppRoles)
            .Distinct()
            .ToList();
    }
    
    private List<string> GetRequiredUserScopes(MethodInfo? targetMethod)
    {
        var attributes = GetCustomAttributesOnClassAndMethod<FunctionAuthorizeAttribute>(targetMethod);
        // Only allow user scopes that are common in class and method level attributes.
        return attributes
            .SelectMany(a => a.UserScopes)
            .Distinct()
            .ToList();
    }

    private List<T> GetCustomAttributesOnClassAndMethod<T>(MethodInfo? targetMethod)
        where T : Attribute
    {
        var methodAttributes = targetMethod?.GetCustomAttributes<T>() ?? new List<T>();
        var classAttributes = targetMethod?.DeclaringType?.GetCustomAttributes<T>() ?? new List<T>();
        return methodAttributes.Concat(classAttributes).ToList();
    }

    private async Task<HttpResponseData> GetResponseAsync(FunctionContext context, HttpStatusCode statusCode, string message)
    {
        var req = await context.GetHttpRequestDataAsync();
        var responseData = req!.CreateResponse();
        responseData.StatusCode = statusCode;
        await responseData.WriteStringAsync(message);
        return responseData;
    }

    private bool TryGetTokenFromHeaders(HttpRequestData requestData, out string? token)
    {
        token = null;
        if(requestData.Headers.TryGetValues("authorization", out var authHeaders))
        {
            var authHeader = authHeaders.First();
            if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                // Scheme is not Bearer
                return false;
            }
    
            token = authHeader.Substring("Bearer ".Length).Trim();
            return true;
        }
        return false;
    }
}