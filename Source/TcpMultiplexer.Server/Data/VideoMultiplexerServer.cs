using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using ModelingEvolution.IO;
using TcpMultiplexer.Server.Common;

namespace TcpMultiplexer.Server.Data;

static class CollectionExtensions
{
    public static void SafeAddUnique<T>(this IList<T> collection, T item)
    {
        lock (collection)
        {
            if(!collection.Contains(item))
                collection.Add(item);
        }
    }

    public static bool SafeRemove<T>(this IList<T> collection, T item)
    {
        lock (collection)
        {
            return collection.Remove(item);
        }
    }
}

public class VideoMultiplexerServer : INotifyPropertyChanged
{
    public record VideoSource(string Host, int Port)
    {
        public override string ToString()
        {
            return $"{Host}:{Port}";
        }
    }

    public IReadOnlyCollection<VideoSource> DisconnectedSources => _disconnected;
    private readonly ObservableCollection<VideoSource> _disconnected;
    private readonly ObservableCollection<VideoStreamReplicator> _streams;
    private readonly string _host;
    private readonly int _port;
    private readonly ILoggerFactory _loggerFactory;
    private readonly TcpListener _listener;
    private readonly ServerConfigProvider _configProvider;
    private DateTime _started;

    public State State
    {
        get => _state;
        private set => SetField(ref _state, value);
    }

    public string Host => _host;

    public int Port => _port;
    public IList<VideoStreamReplicator> Streams => _streams;
    private readonly ILogger<VideoMultiplexerServer> _logger;
    public VideoMultiplexerServer(string host, int port, ILoggerFactory loggerFactory)
    {
        _host = host;
        _port = port;
        _loggerFactory = loggerFactory;
        if (host == "localhost") {
            _host = IPAddress.Loopback.ToString();
        }
        _listener = new TcpListener(IPAddress.Parse(_host), _port)
        {
            Server =
            {
                NoDelay = true
            }
        };
        _streams = new ObservableCollection<VideoStreamReplicator>();
        _configProvider = new ServerConfigProvider();
        _logger = loggerFactory.Create<VideoMultiplexerServer>();
        _disconnected = new ObservableCollection<VideoSource>();
        NxReconnect = DateTime.Now;
    }

    public async Task<VideoStreamReplicator> ConnectVideoSource(string host, int port)
    {
        var streamReplicator = OnConnectVideoSource(host, port);
        await SaveConfig(host, port);
        return streamReplicator;
    }

    private async Task SaveConfig(string host, int port)
    {
        var config = await _configProvider.Get();
        config.Sources.Add($"{host}:{port}");
        await _configProvider.Save();
    }

    private VideoStreamReplicator OnConnectVideoSource(string host, int port)
    {
        VideoStreamReplicator streamReplicator = new VideoStreamReplicator(host, port, _loggerFactory);
        try
        {
            _streams.Add(streamReplicator.Connect());
            streamReplicator.Stopped += OnReplicatorStopped;
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Cannot connect to vide source {Host}:{Port}", host, port);
            streamReplicator.Dispose();
            throw;
        }

        return streamReplicator;
    }

    private void OnReplicatorStopped(object? sender, EventArgs e)
    {
        VideoStreamReplicator replicator = (VideoStreamReplicator)sender;
        replicator.Stopped -= OnReplicatorStopped;
        _streams.Remove(replicator);
       

        var videoSource = new VideoSource(replicator.Host, replicator.Port);
        _disconnected.SafeAddUnique(videoSource);
        replicator.Dispose();
    }

    public DateTime? Started
    {
        get
        {
            if (State != State.Stopped && State != State.Initialized && State != State.Failed)
                return _started;
            return null;
        }
    }

    public DateTime NxReconnect { get; set; }

    public void Start()
    {
        _logger.LogInformation("Video stream replicator is starting...");
        State = State.Starting;
            
        try
        {
            _started = DateTime.Now;
            _tokenSource = new CancellationTokenSource();
            _listener.Start();
                
            Task.Run(OnAcceptEx);
            Task.Run(OnAutoReconnectLoop);
            State = State.Running;
            _logger.LogInformation("Video stream replicator is running.");
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Video stream replicator failed to start");
            State = State.Failed;
        }
    }

    private bool TryConnectVideoSource(VideoSource vs)
    {
        try
        {
            this.OnConnectVideoSource(vs.Host, vs.Port);
            this._disconnected.SafeRemove(vs);

            return true;
        }
        catch (Exception ex)
        {
            _disconnected.SafeAddUnique(vs);
        }
        return false;
    }
    public async Task LoadConfig()
    {
        var serverConfig = await _configProvider.Get();
        foreach (var i in serverConfig.Sources.Distinct())
        {
            string[] p = i.Split(':');
            VideoSource vs = new VideoSource(p[0], int.Parse(p[1]));
            TryConnectVideoSource(vs);
        }
    }
    const int HANDSHAKE_TIMEOUTS_MS = 10000;
    private async Task<VideoStreamReplicator?> FindReplicator(StreamBase ns)
    {
        var toRead = await ns.ReadByteAsync(HANDSHAKE_TIMEOUTS_MS);
        if (toRead.IsNaN) return null;

        var host = await ns.ReadAsciiStringAsync(toRead, HANDSHAKE_TIMEOUTS_MS);
        if (host == null) return null;

        var replicator = _streams.FirstOrDefault(row => row.Host.Equals(host, StringComparison.CurrentCultureIgnoreCase));

        if (replicator != null)
            return replicator;
        

        _logger.LogWarning("Cannot find stream for {RequestedStream}", host);
        return null;
    }
    private bool TryFindSource(NetworkStream ns, out VideoStreamReplicator replicator)
    {
        var bytes = new byte[1024];
        int read = ns.Read(bytes);
        
        string requestedStream = Encoding.UTF8.GetString(bytes[..read]);
        
        replicator = _streams.FirstOrDefault(row => row.Host == requestedStream);

        if (replicator is not null)
        {
            return true;
        }
        
        _logger.LogWarning("Cannot find stream for {RequestedStream}", requestedStream);
        return false;
    }

    private CancellationTokenSource _tokenSource;
    private State _state;
    public bool IsReconnecting { get; private set; }
    public async Task OnAutoReconnectLoop()
    {
        while (true)
        {
            if (_tokenSource.IsCancellationRequested) return;
            try
            {
                if (_disconnected.Count > 0)
                {
                    ReconnectAll();
                }
                var dt = TimeSpan.FromSeconds(30);
                NxReconnect = DateTime.Now.Add(dt);
                await Task.Delay(dt, _tokenSource.Token);
            }
            catch (OperationCanceledException ex)
            {
                return;
            }
        }
    }

    private void ReconnectAll()
    {
        IsReconnecting = true;
        lock (_disconnected) // a bit wide.
        {
            for (int i = 0; i < _disconnected.Count; i++)
            {
                var videoSource = _disconnected[i];
                _logger.LogInformation(
                    $"Trying to reconnect video source: {videoSource.Host}:{videoSource.Port}");
                if (!TryConnectVideoSource(videoSource)) continue;
                
                _logger.LogInformation($"Connected: {videoSource.Host}:{videoSource.Port}");
                i -= 1;
            }
        }

        IsReconnecting = false;
    }
    private async Task OnAcceptEx()
    {
        while (true)
        {
            try
            {
                TcpClient client = await _listener.AcceptTcpClientAsync(_tokenSource.Token);
                var ns = client.GetStream().AsNonBlocking();

                var source = await FindReplicator(ns);
                if (source != null)
                    source.ReplicateTo(ns.Stream, client?.Client?.RemoteEndPoint?.ToString());
                else
                {
                    await ns.DisposeAsync();
                    client.Dispose();
                }
            }
            catch (OperationCanceledException ex)
            {
                State = State.Stopped;
                break;
            }
        }
    }
    private async Task OnAccept()
    {
        while (true)
        {
            try
            {
                TcpClient client = await _listener.AcceptTcpClientAsync(_tokenSource.Token);
                NetworkStream ns = client.GetStream();

                if (TryFindSource(ns, out VideoStreamReplicator source))
                {
                    source.ReplicateTo(ns, client.Client.RemoteEndPoint.ToString());
                }
                else
                {
                    await ns.DisposeAsync();
                    client.Dispose();
                }
            }
            catch(OperationCanceledException ex)
            {
                State = State.Stopped;
                break;
            }
        }
    }

    public void Stop()
    {
        _logger.LogInformation("Video stream replicator is shutting down.");
        this._tokenSource.Cancel();
        foreach (var i in _streams)
            i.Dispose();
        _streams.Clear();
        _listener.Stop();
        _logger.LogInformation("Video stream replicator is stopped.");
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