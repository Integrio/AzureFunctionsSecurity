namespace Integrio.Security.AzureFunctions;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class FunctionAuthorizeAttribute() : Attribute
{
    /// <summary>
    /// Defines which app roles (aka application permissions) are accepted.
    /// </summary>
    public string[] AppRoles { get; set; } = [];

    /// <summary>
    /// Defines which scopes (aka user permissions) are accepted.
    /// </summary>
    public string[] UserScopes { get; set; } = [];
}