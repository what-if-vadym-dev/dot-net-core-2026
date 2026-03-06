using TodoApp.Api.Contracts.Todos;
using TodoApp.Api.Mapping;
using TodoApp.Domain.Abstractions;
using TodoApp.Domain.Models;

namespace TodoApp.Api.GraphQL;

public sealed class TodoGraphQlQuery
{
    public async Task<IReadOnlyList<TodoResponse>> GetTodos(
        [Service] ITodoRepository repository,
        string? search,
        bool? isCompleted,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await repository.QueryAsync(
            new TodoQueryOptions
            {
                Search = search,
                IsCompleted = isCompleted,
                Page = page,
                PageSize = pageSize
            },
            cancellationToken);

        return result.Items.Select(x => x.ToResponseWithoutLinks()).ToList();
    }
}