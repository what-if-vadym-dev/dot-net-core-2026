using Microsoft.Extensions.Logging;
using TodoApp.Domain.Abstractions;
using TodoApp.Domain.Entities;
using TodoApp.Domain.Models;

namespace TodoApp.Infrastructure.Decorators;

public sealed class LoggingTodoRepositoryDecorator(
    ITodoRepository inner,
    ILogger<LoggingTodoRepositoryDecorator> logger) : ITodoRepository
{
    public async Task<PagedResult<TodoItem>> QueryAsync(TodoQueryOptions options, CancellationToken cancellationToken)
    {
        logger.LogInformation("Querying todos with page={Page}, size={PageSize}, sort={SortBy}, desc={Desc}",
            options.Page,
            options.PageSize,
            options.SortBy,
            options.Desc);

        return await inner.QueryAsync(options, cancellationToken);
    }

    public Task<TodoItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        logger.LogInformation("Fetching todo {TodoId}", id);
        return inner.GetByIdAsync(id, cancellationToken);
    }

    public async Task<TodoItem> AddAsync(TodoItem item, CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating todo {Title}", item.Title);
        return await inner.AddAsync(item, cancellationToken);
    }

    public async Task<TodoItem?> UpdateAsync(TodoItem item, CancellationToken cancellationToken)
    {
        logger.LogInformation("Updating todo {TodoId}", item.Id);
        return await inner.UpdateAsync(item, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        logger.LogInformation("Deleting todo {TodoId}", id);
        return await inner.DeleteAsync(id, cancellationToken);
    }
}