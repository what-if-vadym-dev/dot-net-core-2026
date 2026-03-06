namespace TodoApp.Domain.Models;

public sealed class TodoSummaryRow
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public DateTime? DueAtUtc { get; set; }

    public bool IsCompleted { get; set; }

    public int Priority { get; set; }
}