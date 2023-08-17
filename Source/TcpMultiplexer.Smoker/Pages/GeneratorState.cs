namespace TcpMultiplexer.Smoker.Pages;

public enum GeneratorState
{
    Created,
    Listening,
    Stopped,
    Transmitting,
    ClosedByPeer,
    Paused,
}