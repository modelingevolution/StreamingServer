namespace TcpMultiplexer.Smoker.Pages;

public enum PlayerState
{
    Initialized,
    Starting,
    Playing,
    Canceled,
    ClosedByPeer,
    NoData
}