using System.Globalization;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Integrio.Security.AzureFunctions;

public static class FunctionAuthorizationExtensions
{
    private static readonly string entraIdAuthorityUrl = "https://login.microsoftonline.com/{0}/v2.0";
    
    public static IFunctionsWorkerApplicationBuilder UseFunctionAuthorization(
        this IFunctionsWorkerApplicationBuilder builder,
        string configSectionPath)
    {
        builder.Services.AddSingleton<ITokenValidator, TokenValidator>();
        
        var serviceProvider = builder.Services.BuildServiceProvider();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var baseConfigPath = string.IsNullOrEmpty(configSectionPath) ? string.Empty : $"{configSectionPath}:";
        
        var tenantId = configuration.GetValue<string>($"{baseConfigPath}FunctionAuthorization:TenantId"); 

        if (string.IsNullOrEmpty(tenantId))
            throw new ArgumentException($"'{baseConfigPath}FunctionAuthorization:TenantId' configuration value is required");
        
        var validIssuers = configuration.GetValue<string>($"{baseConfigPath}FunctionAuthorization:ValidIssuers")
                               ?.Split(",", StringSplitOptions.TrimEntries) 
                           ?? throw new ArgumentException($"At least one valid issuer is required is required in configuration value '{baseConfigPath}FunctionAuthorization:ValidIssuers'");
        
        if (validIssuers.Length == 0)
            throw new ArgumentException($"At least one valid issuer is required is required in configuration value '{baseConfigPath}FunctionAuthorization:ValidIssuers'");
        
        var validAudiences = configuration.GetValue<string>($"{baseConfigPath}FunctionAuthorization:ValidAudiences")
            ?.Split(",", StringSplitOptions.TrimEntries);
        
        var tokenValidationParameters = CreateTokenValidationParameters(tenantId, validIssuers, validAudiences);

        var disableAuthentication = configuration.GetValue<bool?>("DisableAuthentication") ?? false;
        
        var tokenValidator = serviceProvider.GetRequiredService<ITokenValidator>();
        builder.UseMiddleware<FunctionAuthorizationMiddleware>(_ => new FunctionAuthorizationMiddleware(
            serviceProvider.GetRequiredService<ILogger<FunctionAuthorizationMiddleware>>(),
            tokenValidator, 
            tokenValidationParameters,
            disableAuthentication));

        return builder;
    }

    public static IFunctionsWorkerApplicationBuilder UseFunctionAuthorization(
        this IFunctionsWorkerApplicationBuilder builder,
        string tenantId, 
        string[] validIssuers,
        string[]? validAudiences = null)
    {
        builder.Services.AddSingleton<ITokenValidator, TokenValidator>();
        
        ArgumentException.ThrowIfNullOrEmpty(tenantId, nameof(tenantId));

        if (validIssuers.Length == 0)
            throw new ArgumentException("At least one valid issuer is required", nameof(validIssuers));
        
        var tokenValidationParameters = CreateTokenValidationParameters(tenantId, validIssuers, validAudiences);

        var serviceProvider = builder.Services.BuildServiceProvider();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var disableAuthentication = configuration.GetValue<bool?>("DisableAuthentication") ?? false;

        var tokenValidator = serviceProvider.GetRequiredService<ITokenValidator>();
        builder.UseMiddleware<FunctionAuthorizationMiddleware>(_ =>
            new FunctionAuthorizationMiddleware(
                serviceProvider.GetRequiredService<ILogger<FunctionAuthorizationMiddleware>>(),
                tokenValidator,
                tokenValidationParameters, 
                disableAuthentication));

        return builder;
    }

    private static TokenValidationParameters CreateTokenValidationParameters(
        string tenantId, 
        string[] validIssuers,
        string[]? validAudiences)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuers = validIssuers,
            ValidateAudience = validAudiences != null && validAudiences.Length != 0,
            ValidAudiences = validAudiences,
            ConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                $"{GetAuthority(tenantId)}/.well-known/openid-configuration",
                new OpenIdConnectConfigurationRetriever(), new HttpDocumentRetriever())
        };
        return tokenValidationParameters;
    }


    private static string GetAuthority(string tenantId) =>
        string.Format(CultureInfo.InvariantCulture, entraIdAuthorityUrl, tenantId);
    
    private static IFunctionsWorkerApplicationBuilder UseMiddleware<T>(this IFunctionsWorkerApplicationBuilder builder, Func<IServiceProvider, T> implementationFactory)
        where T : class, IFunctionsWorkerMiddleware
    {
        builder.Services.AddSingleton(implementationFactory);

        builder.Use(next =>
        {
            return context =>
            {
                var middleware = context.InstanceServices.GetRequiredService<T>();
                return middleware.Invoke(context, next);
            };
        });

        return builder;
    }
}