using HotChocolate.Subscriptions;
using TodoApp.Api.Contracts.Todos;
using TodoApp.Api.Mapping;
using TodoApp.Domain.Abstractions;

namespace TodoApp.Api.GraphQL;

public sealed class TodoGraphQlMutation
{
    public async Task<TodoResponse> CreateTodo(
        CreateTodoRequest request,
        [Service] ITodoRepository repository,
        [Service] ITopicEventSender eventSender,
        CancellationToken cancellationToken)
    {
        var entity = request.ToEntity();
        var created = await repository.AddAsync(entity, cancellationToken);
        var response = created.ToResponseWithoutLinks();
        await eventSender.SendAsync(nameof(TodoGraphQlSubscription.OnTodoChanged), response, cancellationToken);
        return response;
    }
}