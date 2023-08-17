using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;

namespace TcpMultiplexer.Smoker.Pages;

public class SmokeVideoPlayer : INotifyPropertyChanged
{
    public Uri Uri { get; }
    public string StreamName { get;  }

    public ulong Received
    {
        get => _received;
        private set
        {
            if (value == _received) return;
            _received = value;
            OnPropertyChanged();
        }
    }

    public PlayerState State
    {
        get => _state;
        private set
        {
            if (value == _state) return;
            _state = value;
            OnPropertyChanged();
        }
    }

    public DateTime Created { get; }

    public ulong Speed
    {
        get => _speed;
        private set
        {
            if (value == _speed) return;
            _speed = value;
            OnPropertyChanged();
        }
    }

    public ulong AvgSpeed
    {
        get => _avgSpeed;
        private set
        {
            if (value == _avgSpeed) return;
            _avgSpeed = value;
            OnPropertyChanged();
        }
    }

    public string Error
    {
        get => _error;
        private set
        {
            if (value == _error) return;
            _error = value;
            OnPropertyChanged();
        }
    }

    public SmokeVideoPlayer(Uri uri, string streamName)
    {
        this.Uri = uri;
        this.StreamName = streamName;
        this.Created = DateTime.Now;
    }

    private CancellationTokenSource _source;
    private ulong _speed;
    private ulong _avgSpeed;
    private PlayerState _state;
    private ulong _received;
    private string _error;

    public void Play()
    {
        State = PlayerState.Starting;
        _source = new CancellationTokenSource();
        Task.Run(OnPlay);
    }
    private async Task OnPlay()
    {
        try
        {
            Error = null;
            using TcpClient client = new TcpClient(Uri.Host, Uri.Port);
            using var ns = client.GetStream();
            var bytes = Encoding.ASCII.GetBytes(StreamName);
            ns.WriteByte((byte)bytes.Length);
            await ns.WriteAsync(bytes, 0, bytes.Length);
            State = PlayerState.Playing;

            var bufferArray = ArrayPool<byte>.Shared.Rent(1024 * 1024);
            var mem = new Memory<byte>(bufferArray);
            await TransferStream(ns, mem);
            ArrayPool<byte>.Shared.Return(bufferArray);
        }
        catch (Exception ex)
        {
            State = PlayerState.ClosedByPeer;
            Error = ex.Message;
        }
    }

    private async Task TransferStream(NetworkStream ns, Memory<byte> mem)
    {
        try
        {
            Stopwatch sw = new Stopwatch();
            Stopwatch gsw = new Stopwatch();
            sw.Start();
            gsw.Start();
            while (true)
            {
                sw.Restart();
                ulong read = 0;
                while (!ns.DataAvailable && sw.ElapsedMilliseconds < 10000) 
                    await Task.Delay(1000 / 60);

                if (!ns.DataAvailable)
                {
                    State = PlayerState.NoData;
                    return;
                }
                
                read = (ulong)await ns.ReadAsync(mem, _source.Token);
                Received += read;
                
                Speed = (ulong)(read / sw.Elapsed.TotalSeconds);
                AvgSpeed = (ulong)(this.Received / gsw.Elapsed.TotalSeconds);
            }
        }
        catch (OperationCanceledException ex)
        {
            State = PlayerState.Canceled;
        }
        catch
        {
            State = PlayerState.ClosedByPeer;
        }
    }

    public void Cancel()
    {
        Error = null;
        _source?.Cancel();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}