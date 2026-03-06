using Microsoft.EntityFrameworkCore;
using TodoApp.Domain.Abstractions;
using TodoApp.Domain.Entities;
using TodoApp.Domain.Models;
using TodoApp.Infrastructure.Data;

namespace TodoApp.Infrastructure.Repositories;

public sealed class EfTodoRepository(TodoDbContext dbContext) : ITodoRepository
{
    public async Task<PagedResult<TodoItem>> QueryAsync(TodoQueryOptions options, CancellationToken cancellationToken)
    {
        options.Normalize();

        IQueryable<TodoItem> query = dbContext.Todos.AsNoTracking();

        if (options.IsCompleted.HasValue)
        {
            query = query.Where(x => x.IsCompleted == options.IsCompleted.Value);
        }

        if (!string.IsNullOrWhiteSpace(options.Search))
        {
            query = query.Where(x =>
                x.Title.Contains(options.Search) ||
                (x.Description != null && x.Description.Contains(options.Search)));
        }

        query = ApplySorting(query, options.SortBy, options.Desc);

        var totalCount = await query.CountAsync(cancellationToken);
        var skip = (options.Page - 1) * options.PageSize;

        var items = await query
            .Skip(skip)
            .Take(options.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<TodoItem>(items, options.Page, options.PageSize, totalCount);
    }

    public Task<TodoItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.Todos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<TodoItem> AddAsync(TodoItem item, CancellationToken cancellationToken)
    {
        item.CreatedAtUtc = DateTime.UtcNow;
        item.UpdatedAtUtc = DateTime.UtcNow;

        dbContext.Todos.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);
        return item;
    }

    public async Task<TodoItem?> UpdateAsync(TodoItem item, CancellationToken cancellationToken)
    {
        var existing = await dbContext.Todos.FirstOrDefaultAsync(x => x.Id == item.Id, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        existing.Title = item.Title;
        existing.Description = item.Description;
        existing.Priority = item.Priority;
        existing.IsCompleted = item.IsCompleted;
        existing.DueAtUtc = item.DueAtUtc;
        existing.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return existing;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Todos.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        dbContext.Todos.Remove(entity);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static IQueryable<TodoItem> ApplySorting(IQueryable<TodoItem> query, string sortBy, bool desc)
    {
        return (sortBy.ToLowerInvariant(), desc) switch
        {
            ("title", false) => query.OrderBy(x => x.Title),
            ("title", true) => query.OrderByDescending(x => x.Title),
            ("priority", false) => query.OrderBy(x => x.Priority),
            ("priority", true) => query.OrderByDescending(x => x.Priority),
            ("dueat", false) => query.OrderBy(x => x.DueAtUtc),
            ("dueat", true) => query.OrderByDescending(x => x.DueAtUtc),
            ("updatedat", false) => query.OrderBy(x => x.UpdatedAtUtc),
            ("updatedat", true) => query.OrderByDescending(x => x.UpdatedAtUtc),
            (_, false) => query.OrderBy(x => x.CreatedAtUtc),
            _ => query.OrderByDescending(x => x.CreatedAtUtc)
        };
    }
}