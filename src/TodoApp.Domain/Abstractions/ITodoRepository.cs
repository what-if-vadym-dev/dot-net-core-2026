using TodoApp.Domain.Entities;
using TodoApp.Domain.Models;

namespace TodoApp.Domain.Abstractions;

public interface ITodoRepository
{
    Task<PagedResult<TodoItem>> QueryAsync(TodoQueryOptions options, CancellationToken cancellationToken);

    Task<TodoItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<TodoItem> AddAsync(TodoItem item, CancellationToken cancellationToken);

    Task<TodoItem?> UpdateAsync(TodoItem item, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}