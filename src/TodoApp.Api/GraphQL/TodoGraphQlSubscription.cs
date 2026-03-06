using TodoApp.Api.Contracts.Todos;

namespace TodoApp.Api.GraphQL;

public sealed class TodoGraphQlSubscription
{
    [Subscribe]
    public TodoResponse OnTodoChanged([EventMessage] TodoResponse todo) => todo;
}