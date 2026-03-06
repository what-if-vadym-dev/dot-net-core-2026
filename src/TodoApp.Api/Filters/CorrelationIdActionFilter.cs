using Microsoft.AspNetCore.Mvc.Filters;

namespace TodoApp.Api.Filters;

public sealed class CorrelationIdActionFilter : IActionFilter
{
    private const string HeaderName = "X-Correlation-Id";

    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var correlationId) ||
            string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString("N");
            context.HttpContext.Request.Headers[HeaderName] = correlationId.ToString();
        }

        context.HttpContext.Items[HeaderName] = correlationId.ToString();
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        if (context.HttpContext.Items.TryGetValue(HeaderName, out var correlationId) && correlationId is string value)
        {
            context.HttpContext.Response.Headers[HeaderName] = value;
        }
    }
}