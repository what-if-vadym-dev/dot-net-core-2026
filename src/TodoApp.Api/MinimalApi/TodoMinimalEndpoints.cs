using Microsoft.AspNetCore.OutputCaching;
using TodoApp.Api.Contracts.Todos;
using TodoApp.Api.Mapping;
using TodoApp.Domain.Abstractions;
using TodoApp.Domain.Models;

namespace TodoApp.Api.MinimalApi;

public static class TodoMinimalEndpoints
{
    public static IEndpointRouteBuilder MapTodoMinimalEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/minimal/todos")
            .WithTags("Todo Minimal API")
            .RequireRateLimiting("fixed");

        group.MapGet("/", async (
            ITodoRepository repository,
            [AsParameters] TodoQueryOptions options,
            CancellationToken cancellationToken) =>
        {
            var result = await repository.QueryAsync(options, cancellationToken);
            return Results.Ok(result.Items.Select(item => item.ToResponseWithoutLinks()));
        })
        .CacheOutput("todos");

        group.MapPost("/", async (
            ITodoRepository repository,
            IOutputCacheStore outputCacheStore,
            CreateTodoRequest request,
            CancellationToken cancellationToken) =>
        {
            var entity = request.ToEntity();
            var created = await repository.AddAsync(entity, cancellationToken);
            await outputCacheStore.EvictByTagAsync("todos", cancellationToken);
            return Results.Created($"/minimal/todos/{created.Id}", created.ToResponseWithoutLinks());
        });

        return endpoints;
    }
}