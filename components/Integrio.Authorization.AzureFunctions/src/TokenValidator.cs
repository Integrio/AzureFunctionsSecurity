using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Integrio.Security.AzureFunctions;

public class TokenValidator : ITokenValidator
{
    private readonly JsonWebTokenHandler _tokenValidator = new();

    public async Task<TokenValidationResult> ValidateTokenAsync(string? jwtToken, TokenValidationParameters tokenValidationParameters)
    {
        return await _tokenValidator.ValidateTokenAsync(jwtToken, tokenValidationParameters);
    }
}