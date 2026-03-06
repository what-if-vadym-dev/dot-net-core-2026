using TodoApp.Api.Contracts.Common;
using TodoApp.Domain.Enums;

namespace TodoApp.Api.Contracts.Todos;

public sealed class TodoResponse
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsCompleted { get; set; }

    public TodoPriority Priority { get; set; }

    public DateTime? DueAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public List<LinkDto> Links { get; set; } = [];
}