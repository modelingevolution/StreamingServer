namespace TcpMultiplexer.Server.Common;

public interface IViewFor<out T>
{
    T Source { get; }
}