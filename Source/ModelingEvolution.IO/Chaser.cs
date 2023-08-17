using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

[assembly: InternalsVisibleTo("ModelingEvolution.IO.Tests")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace ModelingEvolution.IO;
public interface IChaser
{
    int PendingBytes { get; }
    void Start();
    string Identifier { get; }
    ulong WrittenBytes { get; }
    string Started { get;  }
    Task Close();
}
internal sealed class Chaser : IChaser
{
    private readonly Stream _dst;
    private readonly Func<byte, int?> _validStart;
    private readonly CancellationTokenSource _cancellationTokenSource;

    private readonly IMultiplexer _multiplexer;
    private ulong _written;
    private readonly DateTime _started;
    public int PendingBytes => _pendingWrite;
    public ulong WrittenBytes => _written;
    private int _pendingWrite = 0;
    public string Identifier { get; }
    public string Started {
        get
        {
            var dur = DateTime.Now.Subtract(_started);
            return $"{_started:yyyy.MM.dd HH:mm} ({dur.ToString(@"dd\.hh\:mm\:ss")})";
        }
    }

    public async Task Close()
    {
        _cancellationTokenSource.Cancel();
        await _dst.DisposeAsync();
    }

    public Chaser(IMultiplexer multiplexer, Stream dst, Func<byte, int?> validStart, string identifier = null)
    {
        _dst = dst;
        _validStart = validStart;
        _multiplexer = multiplexer;
        _written = 0;
        _cancellationTokenSource = new CancellationTokenSource();
        _started = DateTime.Now;
        Identifier = identifier;
    }
    private async Task OnWrite()
    {
        try
        {
            //TestStream();
            await OnWriteAsync(_dst, _validStart);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Chaser failed." + ex.Message);
            _dst.Close();
            _multiplexer.Disconnect(this);
        }
    }

    private void TestStream()
    {
        ThreadPool.QueueUserWorkItem(x =>
        {
            NetworkStream n = (NetworkStream)this._dst;
            byte[] buffer = new byte[1024];
            while (true)
            {
                if (n.DataAvailable)
                {
                    Console.WriteLine("WTF!");

                    var c = n.Read(buffer);
                    Console.WriteLine(c);
                    Console.WriteLine(Encoding.UTF8.GetString(buffer, 0, c));
                }
                else Thread.Sleep(1000);
            }
        });
    }

    private void FindStartOffset(ref int offset, ref int count, Func<byte, int?> validStart)
    {
        var span = _multiplexer.Buffer().Span;
        int nc = 0;
        for (int i = offset-1; nc < count + 1; i--)
        {
            nc += 1;
            if (i < 0) i = span.Length - 1;

            var ch = validStart(span[i]);
            if (ch != null)
            {
                offset = i + ch.Value;
                count = nc;
                return;
            }
        }

        throw new InvalidOperationException("Could not find valid start :(");
    }

    

    public void Cancel()
    {
        _cancellationTokenSource.Cancel();
    }

    private async Task OnWriteAsync(Stream destination, Func<byte, int?> validStart)
    {
        var offset = _multiplexer.ReadOffset;
        var currentTotal = this._multiplexer.TotalReadBytes;
        var count = (int)Math.Min(currentTotal, (ulong)this._multiplexer.Buffer().Length);


        FindStartOffset(ref offset, ref count, validStart);
        // count is pending bytes. Offset is whenever.

        // this might big number,
        ulong started = currentTotal - (ulong)count;

        while (!_cancellationTokenSource.IsCancellationRequested)
        {
            this._pendingWrite = (int)(_multiplexer.TotalReadBytes - started - (ulong)this._written);
            if (_pendingWrite > 0)
            {
                int inlineLeft = _multiplexer.Buffer().Length - offset;
                if (inlineLeft == 0)
                {
                    offset = 0;
                    inlineLeft = _multiplexer.Buffer().Length;
                }

                count = Math.Min(inlineLeft, _pendingWrite);

                var slice = _multiplexer.Buffer().Slice(offset, count);
                await destination.WriteAsync(slice);
                this._written += (ulong)count;
                offset += count;
            }
            else
            {
                await Task.Delay(1000 / 60);
            }
        }
    }

    public void Start()
    {
        Task.Run(this.OnWrite);
    }
}