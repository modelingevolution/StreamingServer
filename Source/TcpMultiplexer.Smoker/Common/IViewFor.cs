namespace TcpMultiplexer.Smoker.Common;

public interface IViewFor<out T>
{
    T Source { get; }
}