namespace TodoApp.Domain.Models;

public sealed class TodoQueryOptions
{
    public string? Search { get; set; }

    public bool? IsCompleted { get; set; }

    public string SortBy { get; set; } = "createdAt";

    public bool Desc { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    public string? Fields { get; set; }

    public void Normalize()
    {
        if (Page <= 0)
        {
            Page = 1;
        }

        if (PageSize <= 0 || PageSize > 200)
        {
            PageSize = 20;
        }

        SortBy = string.IsNullOrWhiteSpace(SortBy) ? "createdAt" : SortBy.Trim();
        Search = string.IsNullOrWhiteSpace(Search) ? null : Search.Trim();
        Fields = string.IsNullOrWhiteSpace(Fields) ? null : Fields.Trim();
    }
}