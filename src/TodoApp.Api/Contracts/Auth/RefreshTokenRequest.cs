using System.ComponentModel.DataAnnotations;

namespace TodoApp.Api.Contracts.Auth;

public sealed class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}