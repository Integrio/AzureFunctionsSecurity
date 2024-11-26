#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Integrio.Security.AzureFunctions.WebApplication.Runner;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Xunit;

namespace Integrio.Security.AzureFunctions.Tests;

[TestSubject(typeof(FunctionAuthorizationMiddleware))]
public class FunctionAuthorizationMiddlewareTest
{

    [Fact]
    public async Task Invoke_NonHttpTriggeredFunction_CallsNext()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<FunctionAuthorizationMiddleware>>();
        var tokenValidatorMock = new Mock<ITokenValidator>();
        var tokenValidationParameters = new TokenValidationParameters();
        var middleware = new FunctionAuthorizationMiddlewareFake(loggerMock.Object, tokenValidatorMock.Object, tokenValidationParameters, false);
        
        var context = new Mock<FunctionContext>();

        var httpRequestDataFeature = new Mock<IHttpRequestDataFeature>();
        httpRequestDataFeature.Setup(h => h.GetHttpRequestDataAsync(context.Object)).ReturnsAsync((HttpRequestData?)null);
        var invocationFeatures =  new Mock<IInvocationFeatures>();
        invocationFeatures.Setup(i => i.Get<IHttpRequestDataFeature>()).Returns(httpRequestDataFeature.Object);
        
        context.Setup(c => c.Features).Returns(invocationFeatures.Object);
            
        var nextMock = new Mock<FunctionExecutionDelegate>();

        // Act & Assert
        await middleware.Invoke(context.Object, nextMock.Object);
        
        nextMock.Verify(n => n(context.Object), Times.Once);
    }
    
    [Fact]
    public async Task Invoke_WithHttpContextKeyAndDisableAuthenticationTrue_LogsWarningAndCallsNext()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<FunctionAuthorizationMiddleware>>();
        var tokenValidatorMock = new Mock<ITokenValidator>();
        var tokenValidationParameters = new TokenValidationParameters();
        var middleware = new FunctionAuthorizationMiddlewareFake(loggerMock.Object, tokenValidatorMock.Object, tokenValidationParameters, true);
        var context = new Mock<FunctionContext>();
        
        var httpRequestDataFeature = new Mock<IHttpRequestDataFeature>();
        httpRequestDataFeature.Setup(h => h.GetHttpRequestDataAsync(context.Object)).ReturnsAsync(new FakeHttpRequestData(context.Object, new Uri("http://localhost")));
        
        var invocationFeatures =  new Mock<IInvocationFeatures>();
        invocationFeatures.Setup(i => i.Get<IHttpRequestDataFeature>()).Returns(httpRequestDataFeature.Object);

        context.Setup(c => c.Features).Returns(invocationFeatures.Object);
        
        var nextMock = new Mock<FunctionExecutionDelegate>();

        // Act
        await middleware.Invoke(context.Object, nextMock.Object);

        // Assert
        VerifyLoggerMock(loggerMock, LogLevel.Warning, "Authentication is disabled via configuration!", Times.Once);
        nextMock.Verify(n => n(context.Object), Times.Once);
    }
   
    [Fact]
    public async Task Invoke_WithValidToken_CallsNext()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<FunctionAuthorizationMiddleware>>();
        var tokenValidatorMock = new Mock<ITokenValidator>();
        tokenValidatorMock.Setup(t => t.ValidateTokenAsync(It.IsAny<string?>(), It.IsAny<TokenValidationParameters>())).ReturnsAsync(new TokenValidationResult
        {
            IsValid = true,
            ClaimsIdentity = new ClaimsIdentity(new List<Claim> { new Claim("roles", "Reader") })
        });
        
        var tokenValidationParameters = new TokenValidationParameters();
        var middleware = new FunctionAuthorizationMiddlewareFake(loggerMock.Object, tokenValidatorMock.Object, tokenValidationParameters, false);
        
        var context = new Mock<FunctionContext>();

        var fakeHttpRequestData = new FakeHttpRequestData(context.Object, new Uri("http://localhost"));
        fakeHttpRequestData.Headers.Add("Authorization", "Bearer valid-token");
        var httpRequestDataFeature = new Mock<IHttpRequestDataFeature>();
        httpRequestDataFeature.Setup(h => h.GetHttpRequestDataAsync(context.Object)).ReturnsAsync(fakeHttpRequestData);
        
        var invocationFeatures =  new Mock<IInvocationFeatures>();
        invocationFeatures.Setup(i => i.Get<IHttpRequestDataFeature>()).Returns(httpRequestDataFeature.Object);

        context.Setup(c => c.Items).Returns(new Dictionary<object, object>());
        context.Setup(c => c.Features).Returns(invocationFeatures.Object);
        context.Setup(c => c.FunctionDefinition.EntryPoint).Returns("Integrio.Security.AzureFunctions.WebApplication.Runner.TestHttpTrigger.Run");
        context.Setup(c => c.FunctionDefinition.PathToAssembly).Returns(typeof(TestHttpTrigger).Assembly.Location);

        var nextMock = new Mock<FunctionExecutionDelegate>();

        // Act
        await middleware.Invoke(context.Object, nextMock.Object);

        // Assert
        nextMock.Verify(n => n(context.Object), Times.Once);
    }
    
    private static void VerifyLoggerMock<T>(Mock<ILogger<T>> loggerMock, LogLevel logLevel, string message, Func<Times> times)
    {
        loggerMock.Verify(l => l.Log(
            logLevel,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
            It.IsAny<Exception>(),
            It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)!), times);
    }
    
    private HttpRequestData HttpRequestDataSetup(Dictionary<String, StringValues> query, string body)
    {
        var queryItems = query.Aggregate(new NameValueCollection(),
            (seed, current) => {
                seed.Add(current.Key, current.Value);
                return seed;
            });

        var context = new Mock<FunctionContext>();
        var request = new Mock<HttpRequestData>(context.Object);
        var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(body));
        request.Setup(x => x.Body).Returns(memoryStream);
        request.Setup(x => x.Query).Returns(queryItems);

        return request.Object;
    }
    
}