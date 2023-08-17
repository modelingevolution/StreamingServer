namespace TcpMultiplexer.Server.Data;

public class ServerConfig
{
    public List<string> Sources { get; set; }

    public ServerConfig()
    {
        Sources = new List<string>();
    }
}