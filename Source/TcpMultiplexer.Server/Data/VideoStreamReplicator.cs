using System.Collections.Concurrent;
using System.Net.Sockets;
using Microsoft.Extensions.Hosting;
using ModelingEvolution.IO;
using ModelingEvolution.IO.Nal;
#pragma warning disable CS4014

namespace TcpMultiplexer.Server.Data
{
    public class VideoStreamReplicator : IDisposable
    {
        public event EventHandler Stopped;
        private NetworkStream _source;
        private StreamMultiplexer _multiplexer;
        private readonly string _host;
        private readonly int _port;
        private readonly ILoggerFactory _loggerFactory;
        private readonly DateTime _started;
        private TcpClient _client;
        public StreamMultiplexer StreamMultiplexer => _multiplexer;
        public string Host => _host;
        public DateTime Started => _started;
        public int Port => _port;

        public VideoStreamReplicator(string host, int port, ILoggerFactory loggerFactory)
        {
            _host = host;
            this._port = port;
            _loggerFactory = loggerFactory;
            _started = DateTime.Now;
            
        }

        public VideoStreamReplicator Connect()
        {
            if (_client != null) throw new InvalidOperationException("Cannot reuse Replicator.");

            _client = new TcpClient(_host, _port);
            try
            {
                _source = _client.GetStream();
            }
            catch
            { 
                _client.Dispose();
                throw;
            }

            _multiplexer = new StreamMultiplexer(new NonBlockingNetworkStream(_source), _loggerFactory.Create<StreamMultiplexer>());
            _multiplexer.Stopped += OnStreamingStopped;
            _multiplexer.Start();
            return this;
        }

        private void OnStreamingStopped(object? sender, EventArgs e)
        {
            Stopped?.Invoke(this, EventArgs.Empty);
            _client?.Dispose();
            _multiplexer.Stopped -= OnStreamingStopped;
        }

        public void ReplicateTo(Stream ns, string? identifier)
        {
            IDecoder d = new ReverseDecoder();
            _multiplexer.Chase(ns, x => d.Decode(x) == NALType.SPS ? 0 : null, identifier);
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}
