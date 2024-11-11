#nullable enable
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Integrio.Security.AzureFunctions.Runner;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Xunit;

namespace Integrio.Security.AzureFunctions.Tests;

[TestSubject(typeof(FunctionAuthorizationMiddleware))]
public class FunctionAuthorizationMiddlewareTest
{

    [Fact]
    public async Task Invoke_WithUnsupportedHosting_ThrowsNotSupportedException()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<FunctionAuthorizationMiddleware>>();
        var tokenValidatorMock = new Mock<ITokenValidator>();
        var tokenValidationParameters = new TokenValidationParameters();
        var middleware = new FunctionAuthorizationMiddlewareFake(loggerMock.Object, tokenValidatorMock.Object, tokenValidationParameters, false);
        var context = new Mock<FunctionContext>();
        context.Setup(c => c.Items).Returns(new Dictionary<object, object?>()!);

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(() => middleware.Invoke(context.Object, Mock.Of<FunctionExecutionDelegate>()));
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
        var httpContext = new DefaultHttpContext();
        var items = new Dictionary<object, object?> { { "HttpRequestContext", httpContext } };
        context.Setup(c => c.Items).Returns(items!);
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
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Authorization"] = "Bearer valid-token";
        context.Setup(c => c.Items).Returns(new Dictionary<object, object> { { "HttpRequestContext", httpContext } });
        context.Setup(c => c.FunctionDefinition.EntryPoint).Returns("Integrio.Security.AzureFunctions.Runner.TestHttpTrigger.Run");
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
}