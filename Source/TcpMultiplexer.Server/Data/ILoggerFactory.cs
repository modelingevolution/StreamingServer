namespace TcpMultiplexer.Server.Data;

public interface ILoggerFactory
{
    ILogger<T> Create<T>();
}