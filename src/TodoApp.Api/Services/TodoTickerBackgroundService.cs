using TodoApp.Domain.Abstractions;

namespace TodoApp.Api.Services;

public sealed class TodoTickerBackgroundService(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<TodoTickerBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await using var scope = serviceScopeFactory.CreateAsyncScope();
            var todoReadRepository = scope.ServiceProvider.GetRequiredService<ITodoReadRepository>();
            var openCount = await todoReadRepository.CountOpenAsync(stoppingToken);
            logger.LogInformation("TickerQ sample -> open todo count: {OpenCount}", openCount);
            await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);
        }
    }
}