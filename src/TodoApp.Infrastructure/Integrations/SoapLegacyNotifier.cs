using System.Text;
using TodoApp.Domain.Abstractions;
using TodoApp.Domain.Entities;

namespace TodoApp.Infrastructure.Integrations;

public sealed class SoapLegacyNotifier(HttpClient httpClient) : ILegacySoapNotifier
{
    public async Task<string> NotifyTodoCompletedAsync(TodoItem item, CancellationToken cancellationToken)
    {
        var envelope = $"""
<?xml version="1.0" encoding="utf-8"?>
<soap:Envelope xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
               xmlns:xsd="http://www.w3.org/2001/XMLSchema"
               xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/">
  <soap:Body>
    <NotifyTodoCompleted xmlns="http://tempuri.org/">
      <TodoId>{item.Id}</TodoId>
      <Title>{System.Security.SecurityElement.Escape(item.Title)}</Title>
      <CompletedAtUtc>{DateTime.UtcNow:O}</CompletedAtUtc>
    </NotifyTodoCompleted>
  </soap:Body>
</soap:Envelope>
""";

        using var request = new HttpRequestMessage(HttpMethod.Post, "")
        {
            Content = new StringContent(envelope, Encoding.UTF8, "text/xml")
        };

        request.Headers.Add("SOAPAction", "http://tempuri.org/NotifyTodoCompleted");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        return $"Status={(int)response.StatusCode}; BodyLength={body.Length}";
    }
}