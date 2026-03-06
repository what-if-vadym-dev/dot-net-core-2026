using ZiggyCreatures.Caching.Fusion;

namespace TodoApp.Infrastructure.Caching;

public interface IFusionTodoCache
{
    Task<T> GetOrSetAsync<T>(string key, Func<CancellationToken, Task<T>> factory, TimeSpan duration, CancellationToken cancellationToken);
}

public sealed class FusionTodoCache(IFusionCache fusionCache) : IFusionTodoCache
{
    public Task<T> GetOrSetAsync<T>(string key, Func<CancellationToken, Task<T>> factory, TimeSpan duration, CancellationToken cancellationToken)
    {
        return fusionCache.GetOrSetAsync<T>(
            key,
            async (_, ct) => await factory(ct),
            options => options.SetDuration(duration),
            cancellationToken).AsTask();
    }
}