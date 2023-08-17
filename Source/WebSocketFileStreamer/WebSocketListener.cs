using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading.Channels;

namespace TcpMultiplexer.Server;



public class WebSocketListener
{
    private readonly string _host;
    private readonly int _port;
    private readonly Channel<WebSocketInfo> _acceptChannel;
    public WebSocketListener(string host, int port)
    {
        _host = host;
        _port = port;
        
        _acceptChannel = Channel.CreateUnbounded<WebSocketInfo>();
    }

    public async ValueTask<WebSocketInfo> AcceptWebSocketAsync()
    {
        return await _acceptChannel.Reader.ReadAsync();
    }
    public async Task Start()
    {
        var builder = WebApplication.CreateBuilder(new string[]{ "--urls", $"http://{_host}:{_port}"});

        var app = builder.Build();
        
        app.UseWebSockets();
        
        app.UseDefaultFiles();
        app.UseStaticFiles();

        app.AcceptWebSocketAsync(_acceptChannel);

        await app.StartAsync();
    }

}

public record WebSocketInfo(WebSocket WebSocket, TaskCompletionSource<object> TaskCompletionSource);
public static class WebSocketAppExtensions
{
    

    public static void AcceptWebSocketAsync2(this WebApplication app, Channel<WebSocket> acceptChannel)
    {
        // <snippet_AcceptWebSocketAsync>
        app.Use(async (context, next) =>
        {
            if (context.Request.Path == "/ws")
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                    await acceptChannel.Writer.WriteAsync(webSocket);
                }
                else
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                }
            }
            else
            {
                await next(context);
            }

        });
        // </snippet_AcceptWebSocketAsync>
    }

    public static void AcceptWebSocketAsync(this WebApplication app, Channel<WebSocketInfo> acceptChannel)
    {
        // <snippet_AcceptWebSocketAsyncBackgroundSocketProcessor>
        app.Run(async (context) =>
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                var socketFinishedTcs = new TaskCompletionSource<object>();
                WebSocketInfo info = new WebSocketInfo(webSocket, socketFinishedTcs);
                await acceptChannel.Writer.WriteAsync(info);

                await socketFinishedTcs.Task;
            }
        });
        // </snippet_AcceptWebSocketAsyncBackgroundSocketProcessor>
    }

    public static void UseWebSocketsOptionsAllowedOrigins(WebApplication app, params string[] origins)
    {
        // <snippet_UseWebSocketsOptionsAllowedOrigins>
        var webSocketOptions = new WebSocketOptions
        {
            KeepAliveInterval = TimeSpan.FromMinutes(2)
        };

        webSocketOptions.AllowedOrigins.Add("https://client.com");
        webSocketOptions.AllowedOrigins.Add("https://www.client.com");

        app.UseWebSockets(webSocketOptions);
        // </snippet_UseWebSocketsOptionsAllowedOrigins>
    }

    // <snippet_Echo>
    public static async Task Echo(WebSocket webSocket)
    {
        var buffer = new byte[1024 * 64];
        //const string filePath = "C:\\Users\\rafal\\Sources\\WeldingAutomation\\TcpMultiplexer\\Source\\tcpdumper\\bin\\Debug\\net7.0\\file.mp4";
        int transfered = 0;
        //using FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var tcpClient = new TcpClient("192.168.241.3", 2001);
        var fs = tcpClient.GetStream();
        while (true)
        {
            var read = await fs.ReadAsync(buffer, 0, buffer.Length);

            var mem = buffer.AsMemory(0, read);
            await webSocket.SendAsync(mem, WebSocketMessageType.Binary, WebSocketMessageFlags.None, CancellationToken.None);
            transfered += mem.Length;
            Console.WriteLine($"{transfered / 1024} KB");

        }

        var receiveResult = await webSocket.ReceiveAsync(
            new ArraySegment<byte>(buffer), CancellationToken.None);

        while (!receiveResult.CloseStatus.HasValue)
        {
            await webSocket.SendAsync(
                new ArraySegment<byte>(buffer, 0, receiveResult.Count),
                receiveResult.MessageType,
                receiveResult.EndOfMessage,
                CancellationToken.None);

            receiveResult = await webSocket.ReceiveAsync(
                new ArraySegment<byte>(buffer), CancellationToken.None);
        }

        await webSocket.CloseAsync(
            receiveResult.CloseStatus.Value,
            receiveResult.CloseStatusDescription,
            CancellationToken.None);
    }
    // </snippet_Echo>
}