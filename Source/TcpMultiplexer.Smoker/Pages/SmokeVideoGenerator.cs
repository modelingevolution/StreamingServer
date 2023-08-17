using System.Buffers;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using ModelingEvolution.IO.Nal;

namespace TcpMultiplexer.Smoker.Pages;

public class SmokeVideoGenerator : INotifyPropertyChanged
{
    private TcpListener? _listener;
    public SmokeVideoGenerator(int port)
    {
        Port = port;
        Created = DateTime.Now;
        State = GeneratorState.Created;
            
    }

    public DateTime Created { get; }
    public int Port { get; init; }

    public ulong Generated
    {
        get => _generated;
        private set
        {
            if (value == _generated) return;
            _generated = value;
            OnPropertyChanged();
        }
    }

    public ulong Speed { get; private set; }

    public GeneratorState State
    {
        get => _state;
        private set
        {
            if (value == _state) return;
            _state = value;
            OnPropertyChanged();
        }
    }

    public async Task Listen()
    {
        _listener = new TcpListener(Port);
        _listener.Start();
        _source = new CancellationTokenSource();
        State = GeneratorState.Listening;
#pragma warning disable CS4014
        Task.Run(WaitForConnection);
#pragma warning restore CS4014
    }

    private CancellationTokenSource _source;
    private GeneratorState _state;
    private ulong _generated;
    public int Delay { get; set; } = 500;
    private async Task WaitForConnection()
    {
        var megPool = ArrayPool<byte>.Shared.Rent(512 * 1024);
        try
        {
            using var client = await _listener.AcceptTcpClientAsync(_source.Token);
            using var ns = client.GetStream();
            State = GeneratorState.Transmitting;

            Memory<byte> buffer = new Memory<byte>(megPool).Slice(0, 512 * 1024);
            Random random = new Random();
            Stopwatch sw = new Stopwatch();
            sw.Start();
            while (true)
            {
                while (_paused)
                    await Task.Delay(100);

                State = GeneratorState.Transmitting;
                random.NextBytes(buffer.Span);
                buffer.Span[0] = 0;
                buffer.Span[1] = 0;
                buffer.Span[2] = 0;
                buffer.Span[3] = 1;
                buffer.Span[4] = (byte)NALType.SPS;

                await ns.WriteAsync(buffer, _source.Token);
                await ns.FlushAsync(_source.Token);

                Speed = (ulong)(buffer.Length / sw.Elapsed.TotalSeconds);
                Generated += (ulong)buffer.Length;
                sw.Restart();
                await Task.Delay(Delay);
            }
        }
           
        catch (OperationCanceledException ex)
        {
            _listener.Stop();
            _listener = null;
            State = GeneratorState.Stopped;
        }
        catch (Exception ex)
        {
            _listener.Stop();
            _listener = null;
            State = GeneratorState.ClosedByPeer;
            Debug.WriteLine(ex.Message);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(megPool);
        }
    }

    private bool _paused;
    public void Pause()
    {
        _paused = !_paused;
        if(_paused)
            State = GeneratorState.Paused;
    }
    public async Task Stop()
    {
        if (_listener != null)
        {
            _source.Cancel();
        } 
        else 
            State = GeneratorState.Stopped;
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

    public void SpeedUp()
    {
        Delay /= 2;
        if (Delay == 0) Delay = 1;
    }

    public void SpeedDown()
    {
        Delay += 2;
    }
}