using FastEndpoints;
using TodoApp.Api.Contracts.Todos;
using TodoApp.Api.Mapping;
using TodoApp.Domain.Abstractions;

namespace TodoApp.Api.FastEndpoints;

public sealed class CreateTodoFastEndpoint(ITodoRepository repository) : Endpoint<CreateTodoRequest, TodoResponse>
{
    public override void Configure()
    {
        Post("/fast/todos");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "FastEndpoints: create todo";
            s.Description = "Creates a Todo item using FastEndpoints pipeline.";
        });
    }

    public override async Task HandleAsync(CreateTodoRequest request, CancellationToken ct)
    {
        var todo = await repository.AddAsync(request.ToEntity(), ct);
        HttpContext.Response.StatusCode = StatusCodes.Status201Created;
        await HttpContext.Response.WriteAsJsonAsync(todo.ToResponseWithoutLinks(), ct);
    }
}