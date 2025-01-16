using System.Security.Claims;

namespace Integrio.Security.AzureFunctions;

public class ClaimsIdentityProvider : IClaimsIdentityProvider
{
    public ClaimsIdentity? ClaimsIdentity { get; set; }
}