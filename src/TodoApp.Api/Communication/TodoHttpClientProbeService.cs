namespace TodoApp.Api.Communication;

public sealed class TodoHttpClientProbeService(IHttpClientFactory httpClientFactory)
{
    public async Task<string> GetRawTodoAsync(int id, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("todo-http");
        var response = await client.GetAsync($"/todos/{id}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
}