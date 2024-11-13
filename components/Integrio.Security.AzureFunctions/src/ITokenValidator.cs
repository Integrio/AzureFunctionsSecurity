using Microsoft.IdentityModel.Tokens;

namespace Integrio.Security.AzureFunctions;

public interface ITokenValidator
{
    Task<TokenValidationResult> ValidateTokenAsync(string? jwtToken, TokenValidationParameters tokenValidationParameters);
}