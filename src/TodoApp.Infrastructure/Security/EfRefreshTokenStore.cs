using Microsoft.EntityFrameworkCore;
using TodoApp.Domain.Abstractions;
using TodoApp.Domain.Entities;
using TodoApp.Infrastructure.Data;

namespace TodoApp.Infrastructure.Security;

public sealed class EfRefreshTokenStore(TodoDbContext dbContext) : IRefreshTokenStore
{
    public async Task<RefreshToken> CreateAsync(string userId, TimeSpan lifetime, CancellationToken cancellationToken)
    {
        var refreshToken = new RefreshToken
        {
            UserId = userId,
            Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()) + Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
            ExpiresAtUtc = DateTime.UtcNow.Add(lifetime),
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.RefreshTokens.Add(refreshToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return refreshToken;
    }

    public Task<RefreshToken?> GetActiveAsync(string token, CancellationToken cancellationToken)
    {
        return dbContext.RefreshTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Token == token && x.RevokedAtUtc == null && x.ExpiresAtUtc > DateTime.UtcNow, cancellationToken);
    }

    public async Task RevokeAsync(string token, CancellationToken cancellationToken)
    {
        var entry = await dbContext.RefreshTokens.FirstOrDefaultAsync(x => x.Token == token, cancellationToken);
        if (entry is null)
        {
            return;
        }

        entry.RevokedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}