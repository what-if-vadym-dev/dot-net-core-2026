namespace TodoApp.Api.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "TodoApp";

    public string Audience { get; set; } = "TodoApp.Client";

    public string SigningKey { get; set; } = "a-very-long-development-signing-key-change-me";

    public int AccessTokenMinutes { get; set; } = 30;

    public int RefreshTokenDays { get; set; } = 7;
}