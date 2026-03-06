using TodoApp.Domain.Entities;

namespace TodoApp.Domain.Abstractions;

public interface IRefreshTokenStore
{
    Task<RefreshToken> CreateAsync(string userId, TimeSpan lifetime, CancellationToken cancellationToken);

    Task<RefreshToken?> GetActiveAsync(string token, CancellationToken cancellationToken);

    Task RevokeAsync(string token, CancellationToken cancellationToken);
}