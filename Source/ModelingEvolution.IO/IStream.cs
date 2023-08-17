using System.Diagnostics;
using System.Text;

namespace ModelingEvolution.IO;

public abstract class StreamBase : IDisposable
{
    private Stopwatch _timeoutStopper = new Stopwatch();
    public abstract ValueTask<int> ReadAsync(Memory<byte> buffer);
    protected abstract void Dispose(bool disposing);
    
    public async ValueTask<int> ReadAsync(Memory<byte> buffer, int timeoutMiliseconds)
    {
        _timeoutStopper.Restart();
        while (true)
        {
            var read = await ReadAsync(buffer);
            if (read > 0) return read;
            await Task.Delay(1000 / 60);
            if (_timeoutStopper.ElapsedMilliseconds <= timeoutMiliseconds) continue;
            
            return 0;
        }
    }

    
    public async Task<string?> ReadAsciiStringAsync(int len, int timeoutMiliSeconds)
    {
        Memory<byte> buffer = new Memory<byte>(new byte[len]);
        var ret = await ReadAsync(buffer, timeoutMiliSeconds); // this might fail
        if (ret != len)
            return null;

        string host = Encoding.ASCII.GetString(buffer.Span);
        return host;
    }
    public void Dispose()
    {
        Dispose(true);
    }

    public async Task<ByteResult> ReadByteAsync(int timeoutMiliseconds)
    {
        Memory<byte> buffer = new Memory<byte>(new byte[1]);
        var c = await this.ReadAsync(buffer, timeoutMiliseconds);
        if (c == 1)
            return buffer.Span[0];
        return ByteResult.NaN;
    }
}