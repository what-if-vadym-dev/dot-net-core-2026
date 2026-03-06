using TodoApp.Domain.Models;

namespace TodoApp.Domain.Abstractions;

public interface ITodoReadRepository
{
    Task<int> CountOpenAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<TodoSummaryRow>> GetOverdueAsync(int take, CancellationToken cancellationToken);
}