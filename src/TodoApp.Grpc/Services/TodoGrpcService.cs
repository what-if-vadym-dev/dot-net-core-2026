using Grpc.Core;
using TodoApp.Domain.Abstractions;
using TodoApp.Domain.Models;

namespace TodoApp.Grpc.Services;

public sealed class TodoGrpcService(ITodoRepository repository) : TodoService.TodoServiceBase
{
    public override async Task<GetTodoReply> GetTodo(GetTodoRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.Id, out var id))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid todo id format."));
        }

        var todo = await repository.GetByIdAsync(id, context.CancellationToken);
        if (todo is null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Todo not found."));
        }

        return new GetTodoReply
        {
            Item = new TodoGrpcItem
            {
                Id = todo.Id.ToString(),
                Title = todo.Title,
                Description = todo.Description ?? string.Empty,
                IsCompleted = todo.IsCompleted,
                Priority = (int)todo.Priority,
                DueAtUtc = todo.DueAtUtc?.ToString("O") ?? string.Empty,
                CreatedAtUtc = todo.CreatedAtUtc.ToString("O"),
                UpdatedAtUtc = todo.UpdatedAtUtc.ToString("O")
            }
        };
    }

    public override async Task<ListTodosReply> ListTodos(ListTodosRequest request, ServerCallContext context)
    {
        var options = new TodoQueryOptions
        {
            Page = request.Page <= 0 ? 1 : request.Page,
            PageSize = request.PageSize <= 0 ? 20 : request.PageSize,
            IsCompleted = request.IncludeCompleted ? null : false,
            SortBy = "createdAt",
            Desc = true
        };

        var result = await repository.QueryAsync(options, context.CancellationToken);

        var reply = new ListTodosReply
        {
            TotalCount = result.TotalCount
        };

        reply.Items.AddRange(result.Items.Select(todo => new TodoGrpcItem
        {
            Id = todo.Id.ToString(),
            Title = todo.Title,
            Description = todo.Description ?? string.Empty,
            IsCompleted = todo.IsCompleted,
            Priority = (int)todo.Priority,
            DueAtUtc = todo.DueAtUtc?.ToString("O") ?? string.Empty,
            CreatedAtUtc = todo.CreatedAtUtc.ToString("O"),
            UpdatedAtUtc = todo.UpdatedAtUtc.ToString("O")
        }));

        return reply;
    }
}