using Microsoft.AspNetCore.Mvc;
using TodoApp.Api.Contracts.Common;
using TodoApp.Api.Contracts.Todos;
using TodoApp.Domain.Entities;

namespace TodoApp.Api.Mapping;

public static class TodoMappingExtensions
{
    public static TodoItem ToEntity(this CreateTodoRequest request)
    {
        return new TodoItem
        {
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            Priority = request.Priority,
            DueAtUtc = request.DueAtUtc
        };
    }

    public static TodoItem ToEntity(this UpdateTodoRequest request, Guid id)
    {
        return new TodoItem
        {
            Id = id,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            Priority = request.Priority,
            IsCompleted = request.IsCompleted,
            DueAtUtc = request.DueAtUtc
        };
    }

    public static TodoResponse ToResponse(this TodoItem item, IUrlHelper url, string version)
    {
        var response = item.ToResponseWithoutLinks();
        response.Links =
        [
            new LinkDto("self", url.Link("GetTodoById", new { version, id = item.Id }) ?? $"/api/v{version}/todos/{item.Id}", "GET"),
            new LinkDto("update", $"/api/v{version}/todos/{item.Id}", "PUT"),
            new LinkDto("delete", $"/api/v{version}/todos/{item.Id}", "DELETE"),
            new LinkDto("complete", $"/api/v{version}/todos/{item.Id}/complete", "POST")
        ];

        return response;
    }

    public static TodoResponse ToResponseWithoutLinks(this TodoItem item)
    {
        return new TodoResponse
        {
            Id = item.Id,
            Title = item.Title,
            Description = item.Description,
            IsCompleted = item.IsCompleted,
            Priority = item.Priority,
            DueAtUtc = item.DueAtUtc,
            CreatedAtUtc = item.CreatedAtUtc,
            UpdatedAtUtc = item.UpdatedAtUtc
        };
    }

    public static object Shape(this TodoResponse response, string? fields)
    {
        if (string.IsNullOrWhiteSpace(fields))
        {
            return response;
        }

        var selected = fields
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.ToLowerInvariant())
            .ToHashSet();

        var dict = new Dictionary<string, object?>();

        if (selected.Contains("id")) dict["id"] = response.Id;
        if (selected.Contains("title")) dict["title"] = response.Title;
        if (selected.Contains("description")) dict["description"] = response.Description;
        if (selected.Contains("iscompleted")) dict["isCompleted"] = response.IsCompleted;
        if (selected.Contains("priority")) dict["priority"] = response.Priority;
        if (selected.Contains("dueatutc")) dict["dueAtUtc"] = response.DueAtUtc;
        if (selected.Contains("createdatutc")) dict["createdAtUtc"] = response.CreatedAtUtc;
        if (selected.Contains("updatedatutc")) dict["updatedAtUtc"] = response.UpdatedAtUtc;
        if (selected.Contains("links")) dict["links"] = response.Links;

        return dict.Count == 0 ? response : dict;
    }
}