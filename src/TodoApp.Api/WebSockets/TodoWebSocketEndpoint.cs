using System.Net.WebSockets;
using System.Text;

namespace TodoApp.Api.WebSockets;

public static class TodoWebSocketEndpoint
{
    public static async Task HandleAsync(HttpContext context)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        using var socket = await context.WebSockets.AcceptWebSocketAsync();
        var buffer = new byte[1024 * 4];

        while (socket.State == WebSocketState.Open)
        {
            var result = await socket.ReceiveAsync(buffer, context.RequestAborted);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Bye", context.RequestAborted);
                break;
            }

            var input = Encoding.UTF8.GetString(buffer, 0, result.Count);
            var output = Encoding.UTF8.GetBytes($"todo-ws-echo:{input}");
            await socket.SendAsync(output, WebSocketMessageType.Text, true, context.RequestAborted);
        }
    }
}