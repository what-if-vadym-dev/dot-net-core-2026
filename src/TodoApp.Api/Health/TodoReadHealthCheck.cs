using Microsoft.Extensions.Diagnostics.HealthChecks;
using TodoApp.Domain.Abstractions;

namespace TodoApp.Api.Health;

public sealed class TodoReadHealthCheck(ITodoReadRepository readRepository) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var open = await readRepository.CountOpenAsync(cancellationToken);
            return HealthCheckResult.Healthy($"Open todos: {open}");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Failed to query todos through Dapper.", exception);
        }
    }
}