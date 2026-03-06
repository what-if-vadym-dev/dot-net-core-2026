namespace TodoApp.Api.Options;

public sealed class OidcOptions
{
    public const string SectionName = "Oidc";

    public string Authority { get; set; } = "https://localhost:8443/realms/master";

    public string ClientId { get; set; } = "todo-api";

    public string ClientSecret { get; set; } = "todo-secret";

    public string KeycloakAudience { get; set; } = "account";
}