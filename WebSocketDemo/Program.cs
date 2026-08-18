using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;


// Main program setup
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ConnectionManager>();

var app = builder.Build();

app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30)
});

app.Map("/ws", async (HttpContext context, ConnectionManager manager) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }

    using WebSocket socket = await context.WebSockets.AcceptWebSocketAsync();
    string connectionId = manager.Add(socket);

    try
    {
        while (socket.State == WebSocketState.Open)
        {
            string? message = await ReceiveTextAsync(
                socket, 64 * 1024, context.RequestAborted);

            if (message is null) break;

            byte[] reply = Encoding.UTF8.GetBytes(
                $"Server received: {message}");

            await socket.SendAsync(
                reply,
                WebSocketMessageType.Text,
                endOfMessage: true,
                context.RequestAborted);
        }
    }
    catch (OperationCanceledException) { }
    catch (WebSocketException ex)
    {
        app.Logger.LogWarning(ex,
            "WebSocket failure for {ConnectionId}", connectionId);
    }
    finally
    {
        manager.Remove(connectionId, out _);

        if (socket.State is WebSocketState.Open or
            WebSocketState.CloseReceived)
        {
            await socket.CloseAsync(
                WebSocketCloseStatus.NormalClosure,
                "Closing", CancellationToken.None);
        }
    }
});

app.MapGet("/", () => "Hello World!");

app.Run();

// Helper function to receive full messages, handling fragmentation
static async Task<string?> ReceiveTextAsync(
    WebSocket socket, int maximumBytes, CancellationToken token)
{
    byte[] buffer = new byte[4096];
    using var stream = new MemoryStream();

    while (true)
    {
        WebSocketReceiveResult result = await socket.ReceiveAsync(
            buffer, token);

        if (result.MessageType == WebSocketMessageType.Close)
            return null;

        if (result.MessageType != WebSocketMessageType.Text)
            throw new WebSocketException("Only text is supported.");

        stream.Write(buffer, 0, result.Count);

        if (stream.Length > maximumBytes)
            throw new WebSocketException("Message is too large.");

        if (result.EndOfMessage)
            return Encoding.UTF8.GetString(stream.ToArray());
    }
}


// Connection Manager
public sealed class ConnectionManager
{
    private readonly ConcurrentDictionary<string, WebSocket> _connections = new();

    public string Add(WebSocket socket)
    {
        string id = Guid.NewGuid().ToString("N");
        _connections[id] = socket;
        return id;
    }

    public IReadOnlyCollection<WebSocket> GetAll() =>
        _connections.Values.ToList();

    public bool Remove(string id, out WebSocket? socket) =>
        _connections.TryRemove(id, out socket);
}