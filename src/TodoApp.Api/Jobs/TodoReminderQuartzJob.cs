using Quartz;
using TodoApp.Domain.Abstractions;

namespace TodoApp.Api.Jobs;

public sealed class TodoReminderQuartzJob(
    ITodoReadRepository todoReadRepository,
    ILogger<TodoReminderQuartzJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var overdue = await todoReadRepository.GetOverdueAsync(5, context.CancellationToken);
        if (overdue.Count == 0)
        {
            logger.LogInformation("Quartz check: no overdue todos.");
            return;
        }

        logger.LogWarning("Quartz check: overdue todos found: {Count}", overdue.Count);
    }
}