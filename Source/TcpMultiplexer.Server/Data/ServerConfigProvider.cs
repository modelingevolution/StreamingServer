using System.Text.Json;

namespace TcpMultiplexer.Server.Data;

public class ServerConfigProvider
{
    private ServerConfig? _config;

    public ServerConfigProvider()
    {
        _config = null;
    }

    private const string file = "server-state.json";
    public async Task<ServerConfig?> Get()
    {
        if(_config!=null)  return _config;
        if (File.Exists(file))
        {
            string content = await File.ReadAllTextAsync(file);
            _config = JsonSerializer.Deserialize<ServerConfig>(content);
        }
        else _config = new ServerConfig();

        return _config;
    }

    public async Task Save()
    {
        var content = JsonSerializer.Serialize(_config);
        await File.WriteAllTextAsync(file, content);
    }
}