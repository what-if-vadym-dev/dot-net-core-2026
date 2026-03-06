namespace TodoApp.Domain.Abstractions;

public interface ITokenService
{
    string CreateAccessToken(string userId, string userName, IEnumerable<string> roles);
}