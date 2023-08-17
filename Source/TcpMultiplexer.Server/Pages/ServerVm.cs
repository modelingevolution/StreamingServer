using ModelingEvolution.IO;
using TcpMultiplexer.Server.Common;
using TcpMultiplexer.Server.Data;

namespace TcpMultiplexer.Server.Pages
{
    public class ServerVm
    {
        private List<string> _errors = new List<string>();
        public IList<string> Erros => _errors;
        private readonly VideoMultiplexerServer _server;
        public bool IsStartEnabled => _server.State == State.Initialized || _server.State == State.Stopped;
        public bool IsStopEnabled => _server.State == State.Running;
        public VideoMultiplexerServer Server => _server;
        
        public string Started
        {
            get
            {
                var started = _server.Started;
                if (started != null)
                {
                    var dur = DateTime.Now.Subtract(started.Value);
                    return $"{started.Value:yyyy.MM.dd HH:mm} ({dur.ToString(@"dd\.hh\:mm\:ss")})";
                }
                else return "-";
            }
        }
        public ServerVm(VideoMultiplexerServer server)
        {
            _server = server;
            Items = new ObservableCollectionView<ReplicatorVm, VideoStreamReplicator>(x=>new ReplicatorVm(x), this._server.Streams);
        }
        public async Task Start()
        {
            
            if (_server.State == State.Initialized)
            {
                try
                {
                    await _server.LoadConfig();
                }
                catch(Exception ex)
                {
                    _errors.Add(ex.Message);
                }
            }
            _server.Start();
        }

        public async Task Stop()
        {
            _server.Stop();
        }
        public IObservableCollectionView<ReplicatorVm, VideoStreamReplicator> Items { get; private set; }
        public string BindAddress => $"{_server.Host}:{_server.Port}";

        public string AllocatedBuffersBytes
        {
            get
            {
                long size = 0;
                try
                {
                    for (int i = 0; i < _server.Streams.Count; i++)
                    {
                        var stream = _server.Streams[i];
                        size += stream.StreamMultiplexer.BufferLength;
                    }
                }
                catch (IndexOutOfRangeException)
                {
                    // We don't care.
                }

                return size.WithSizeSuffix(1);
            }
        }

        public string ReconnectStatus
        {
            get
            {
                if (_server.IsReconnecting)
                    return "Reconnecting...";
                return this._server.NxReconnect.Subtract(DateTime.Now).ToString(@"mm\:ss");
            }
        }
    }

    public class ReplicatorVm : IViewFor<VideoStreamReplicator>, IEquatable<ReplicatorVm>
    {
        
        private readonly SpeedVm _transferSpeed;
        private readonly VideoStreamReplicator _source;
        public StreamMultiplexer StreamMultiplexer => _source.StreamMultiplexer;

        public string Host => _source.Host;

        public int Port => _source.Port;

        public ReplicatorVm(VideoStreamReplicator source)
        {
            _source = source;
            _transferSpeed = new SpeedVm();
        }
        public string Speed => _transferSpeed.Calculate(Source.StreamMultiplexer.TotalReadBytes);

        public VideoStreamReplicator Source
        {
            get => _source;
        }

        public string Started
        {
            get
            {
                var dur = DateTime.Now.Subtract(_source.Started);
                return $"{_source.Started:yyyy.MM.dd HH:mm} ({dur.ToString(@"dd\.hh\:mm\:ss")})";
            }
        }

        public bool Equals(ReplicatorVm? other)
        {
            if(ReferenceEquals(this, other)) return true;
            if (other == null) return false;

            return this.Host == other.Host && this.Port == other.Port;
        }
    }
}
