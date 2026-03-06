using TodoApp.Domain.Abstractions;

namespace TodoApp.Api.Jobs;

public sealed class HangfireTodoJobs(ITodoReadRepository todoReadRepository, ILogger<HangfireTodoJobs> logger)
{
    public async Task LogOpenTodoCountAsync(CancellationToken cancellationToken)
    {
        var openCount = await todoReadRepository.CountOpenAsync(cancellationToken);
        logger.LogInformation("Hangfire recurring job -> open todos: {OpenCount}", openCount);
    }
}