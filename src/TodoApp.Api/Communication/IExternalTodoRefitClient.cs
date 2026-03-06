using Refit;

namespace TodoApp.Api.Communication;

public interface IExternalTodoRefitClient
{
    [Get("/todos/{id}")]
    Task<ExternalTodoDto> GetTodoAsync(int id, CancellationToken cancellationToken = default);
}

public sealed record ExternalTodoDto(int UserId, int Id, string Title, bool Completed);