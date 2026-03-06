using TodoApp.Domain.Entities;

namespace TodoApp.Domain.Abstractions;

public interface ILegacySoapNotifier
{
    Task<string> NotifyTodoCompletedAsync(TodoItem item, CancellationToken cancellationToken);
}