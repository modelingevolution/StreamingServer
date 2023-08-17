using TcpMultiplexer.Server;

namespace WebSocketFileStreamer
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            //var builder = WebApplication.CreateBuilder(args);
            //var app = builder.Build();
            //app.UseStaticFiles();
            //await app.StartAsync();

            WebSocketListener l = new WebSocketListener("localhost", 1234);
            l.Start();

            while (true)
            {
                var w = await l.AcceptWebSocketAsync();
                await WebSocketAppExtensions.Echo(w.WebSocket);
                w.TaskCompletionSource.SetResult(null);
            }
        }
    }
}