using System.Security.Claims;

namespace Integrio.Security.AzureFunctions;

public interface IClaimsIdentityProvider
{
    ClaimsIdentity? ClaimsIdentity { get; set; }
}