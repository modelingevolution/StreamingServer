using System.Net.Sockets;

namespace ModelingEvolution.IO;


public class NonBlockingNetworkStream : StreamBase
{
    private readonly NetworkStream _stream;

    public NonBlockingNetworkStream(NetworkStream stream)
    {
        _stream = stream;
    }

    public NetworkStream Stream => _stream;

    protected override void Dispose(bool disposing)
    {
        _stream.Dispose();
    }
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer)
    {
        if(_stream.DataAvailable)
            return await _stream.ReadAsync(buffer);
        return 0;
    }

    public async Task DisposeAsync()
    {
        await _stream.DisposeAsync();
    }
}