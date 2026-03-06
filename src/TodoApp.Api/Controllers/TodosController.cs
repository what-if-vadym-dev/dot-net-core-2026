using Asp.Versioning;
using HotChocolate.Subscriptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;
using TodoApp.Api.Contracts.Common;
using TodoApp.Api.Contracts.Todos;
using TodoApp.Api.GraphQL;
using TodoApp.Api.Hubs;
using TodoApp.Api.Mapping;
using TodoApp.Api.Services;
using TodoApp.Domain.Abstractions;
using TodoApp.Domain.Models;

namespace TodoApp.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[EnableRateLimiting("fixed")]
[Route("api/v{version:apiVersion}/todos")]
public sealed class TodosController(
    ITodoRepository repository,
    ILegacySoapNotifier soapNotifier,
    IOutputCacheStore outputCacheStore,
    CachedTodoQueryService cachedQueryService,
    IHubContext<TodoHub> hubContext,
    ITopicEventSender eventSender) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    [OutputCache(PolicyName = "todos")]
    [ProducesResponseType(typeof(PagedResponse<object>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<object>>> GetTodos([FromQuery] TodoQueryOptions query, CancellationToken cancellationToken)
    {
        var version = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1";
        var response = await cachedQueryService.QueryAsync(query, version, Url, cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:guid}", Name = "GetTodoById")]
    [AllowAnonymous]
    [OutputCache(PolicyName = "todos")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<object>> GetById(Guid id, [FromQuery] string? fields, CancellationToken cancellationToken)
    {
        var todo = await repository.GetByIdAsync(id, cancellationToken);
        if (todo is null)
        {
            return NotFound();
        }

        var version = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1";
        return Ok(todo.ToResponse(Url, version).Shape(fields));
    }

    [HttpPost]
    [Authorize(Policy = "ApiUser")]
    [ProducesResponseType(typeof(TodoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TodoResponse>> Create([FromBody] CreateTodoRequest request, CancellationToken cancellationToken)
    {
        var created = await repository.AddAsync(request.ToEntity(), cancellationToken);
        await outputCacheStore.EvictByTagAsync("todos", cancellationToken);

        var version = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1";
        var response = created.ToResponse(Url, version);
        await eventSender.SendAsync(nameof(TodoGraphQlSubscription.OnTodoChanged), response, cancellationToken);

        return CreatedAtRoute("GetTodoById", new { version, id = created.Id }, response);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "ApiUser")]
    [ProducesResponseType(typeof(TodoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TodoResponse>> Update(Guid id, [FromBody] UpdateTodoRequest request, CancellationToken cancellationToken)
    {
        var updated = await repository.UpdateAsync(request.ToEntity(id), cancellationToken);
        if (updated is null)
        {
            return NotFound();
        }

        await outputCacheStore.EvictByTagAsync("todos", cancellationToken);
        var version = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1";
        var response = updated.ToResponse(Url, version);
        await hubContext.Clients.All.SendAsync("todo-updated", response, cancellationToken);
        await eventSender.SendAsync(nameof(TodoGraphQlSubscription.OnTodoChanged), response, cancellationToken);

        return Ok(response);
    }

    [HttpPost("{id:guid}/complete")]
    [Authorize(Policy = "ApiUser")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<object>> Complete(Guid id, CancellationToken cancellationToken)
    {
        var todo = await repository.GetByIdAsync(id, cancellationToken);
        if (todo is null)
        {
            return NotFound();
        }

        todo.IsCompleted = true;
        var updated = await repository.UpdateAsync(todo, cancellationToken);
        if (updated is null)
        {
            return NotFound();
        }

        var soapResult = await soapNotifier.NotifyTodoCompletedAsync(updated, cancellationToken);
        await outputCacheStore.EvictByTagAsync("todos", cancellationToken);

        var version = HttpContext.GetRequestedApiVersion()?.ToString() ?? "1";
        var response = updated.ToResponse(Url, version);
        await hubContext.Clients.All.SendAsync("todo-completed", response, cancellationToken);
        await eventSender.SendAsync(nameof(TodoGraphQlSubscription.OnTodoChanged), response, cancellationToken);

        return Ok(new
        {
            todo = response,
            soap = soapResult
        });
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "ApiUser")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await repository.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound();
        }

        await outputCacheStore.EvictByTagAsync("todos", cancellationToken);
        return NoContent();
    }
}