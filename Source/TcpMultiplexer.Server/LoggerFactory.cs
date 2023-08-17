using ILoggerFactory = TcpMultiplexer.Server.Data.ILoggerFactory;

namespace TcpMultiplexer.Server;

class LoggerFactory : ILoggerFactory
{
    private readonly IServiceProvider _serviceProvider;

    public LoggerFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public ILogger<T> Create<T>()
    {
        return _serviceProvider.GetRequiredService<ILogger<T>>();
    }
}