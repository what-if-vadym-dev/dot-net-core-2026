using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using TodoApp.Api.Communication;

namespace TodoApp.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/communication")]
public sealed class ApiCommunicationController(
    IExternalTodoRefitClient refitClient,
    TodoRestSharpClient restSharpClient,
    TodoHttpClientProbeService httpClientProbeService) : ControllerBase
{
    [HttpGet("refit/{id:int}")]
    public async Task<ActionResult<ExternalTodoDto>> ThroughRefit(int id, CancellationToken cancellationToken)
    {
        var dto = await refitClient.GetTodoAsync(id, cancellationToken);
        return Ok(dto);
    }

    [HttpGet("restsharp/{id:int}")]
    public async Task<ActionResult<ExternalTodoDto>> ThroughRestSharp(int id, CancellationToken cancellationToken)
    {
        var dto = await restSharpClient.GetTodoAsync(id, cancellationToken);
        if (dto is null)
        {
            return NotFound();
        }

        return Ok(dto);
    }

    [HttpGet("httpclient/{id:int}")]
    public async Task<ActionResult<string>> ThroughHttpClientFactory(int id, CancellationToken cancellationToken)
    {
        var payload = await httpClientProbeService.GetRawTodoAsync(id, cancellationToken);
        return Ok(payload);
    }
}