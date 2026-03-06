using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.AspNetCore.Mvc;
using TodoApp.Api.Contracts.Common;
using TodoApp.Api.Contracts.Todos;
using TodoApp.Api.Mapping;
using TodoApp.Domain.Abstractions;
using TodoApp.Domain.Models;
using TodoApp.Infrastructure.Caching;

namespace TodoApp.Api.Services;

public sealed class CachedTodoQueryService(
    ITodoRepository repository,
    HybridCache hybridCache,
    IFusionTodoCache fusionTodoCache)
{
    public async Task<PagedResponse<object>> QueryAsync(TodoQueryOptions options, string apiVersion, IUrlHelper url, CancellationToken cancellationToken)
    {
        options.Normalize();
        var cacheKey = $"todos:{options.Search}:{options.IsCompleted}:{options.SortBy}:{options.Desc}:{options.Page}:{options.PageSize}:{options.Fields}";

        var paged = await hybridCache.GetOrCreateAsync(
            cacheKey,
            async cancel => await fusionTodoCache.GetOrSetAsync(
                cacheKey,
                innerCancel => repository.QueryAsync(options, innerCancel),
                TimeSpan.FromSeconds(30),
                cancel),
            cancellationToken: cancellationToken,
            tags: ["todos"]);

        var shapedItems = paged.Items
            .Select(todo => todo.ToResponse(url, apiVersion).Shape(options.Fields))
            .ToList();

        return new PagedResponse<object>(
            shapedItems,
            paged.Page,
            paged.PageSize,
            paged.TotalCount,
            paged.TotalPages);
    }
}