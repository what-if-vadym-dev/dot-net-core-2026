using RestSharp;

namespace TodoApp.Api.Communication;

public sealed class TodoRestSharpClient(IConfiguration configuration)
{
    public async Task<ExternalTodoDto?> GetTodoAsync(int id, CancellationToken cancellationToken)
    {
        var baseUrl = configuration["ExternalApi:BaseUrl"] ?? "https://jsonplaceholder.typicode.com";
        var client = new RestClient(baseUrl);
        var request = new RestRequest($"/todos/{id}", Method.Get);
        var response = await client.ExecuteAsync<ExternalTodoDto>(request, cancellationToken);
        return response.Data;
    }
}