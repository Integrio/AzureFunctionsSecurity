namespace Integrio.Security.AzureFunctions;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class FunctionAuthorizeAttribute(params string[] appRoles) : Attribute
{
    /// <summary>
    /// Defines which app roles (aka application permissions) are accepted.
    /// </summary>
    public string[] AppRoles { get; } = appRoles;
}