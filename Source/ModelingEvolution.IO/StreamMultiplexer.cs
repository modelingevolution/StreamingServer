using System;
using System.Buffers;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace ModelingEvolution.IO;

public class StreamMultiplexer : IMultiplexer
{
    public event EventHandler Stopped; 
    public const int BUFFER_SIZE = 10 * 1024 * 1024;
    public const int CHUNK = 1024;
    private Memory<byte> _buffer;
    private readonly List<IChaser> _chasers;
    private readonly StreamBase _source;
    private readonly ILogger<StreamMultiplexer> _logger;
    //private Thread _reader;
    private int _readOffset;
    private bool _stopped;

    Memory<byte> IMultiplexer.Buffer()
    {
        return _buffer;
    }

    int IMultiplexer.ReadOffset
    {
        get { return _readOffset; }
    }

    /// <summary>
    /// Contains number of bytes read in _buffer. This is if we written 2 times buffer size, than it will be 2x size of the buffer.
    /// It doesn't contain informaiton about current offset in the current buffer - this is in _readOffset.
    /// </summary>
    private ulong _totalBuffersRead;
    public ulong TotalReadBytes => _totalBuffersRead + (ulong)_readOffset;
    
    public IReadOnlyList<IChaser> Chasers => _chasers.AsReadOnly();
    private byte[] _sharedBuffer;
    public StreamMultiplexer(StreamBase source, ILogger<StreamMultiplexer> logger)
    {
        _sharedBuffer = ArrayPool<byte>.Shared.Rent(BUFFER_SIZE);
        _buffer = _sharedBuffer.AsMemory(0,BUFFER_SIZE);
        _chasers = new List<IChaser>();
        _source = source;
        _logger = logger;
    }

    public int ClientCount => _chasers.Count;
    public long BufferLength  => _buffer.Length;

    public void Start()
    {
        Task.Run(OnReadAsync);
    }

   

    void IMultiplexer.Disconnect(IChaser chaser)
    {
        _chasers.Remove(chaser);
    }

    public IChaser Chase(Stream destination, Func<byte, int?> validStart = null, string identifier = null)
    {
        validStart ??= x => 0;
        
        var chaser = new Chaser(this,destination, validStart, identifier);
        _chasers.Add(chaser);
        chaser.Start();
        return chaser;
    }

    

    private async Task OnReadAsync()
    {
        try
        {
            while (true)
            {
                var left = _buffer.Length - _readOffset;
                var count = Math.Min(left, CHUNK);

                var bufferChunk = _buffer.Slice(_readOffset, count);
                var read = await _source.ReadAsync(bufferChunk, 10000);

                if (read == 0)
                {
                    await Close();
                    return;
                }

                var offset = _readOffset + read;
                if (offset == _buffer.Length)
                {
                    _readOffset = 0;
                    Interlocked.Add(ref _totalBuffersRead, (ulong)_buffer.Length);
                }
                else
                {
                    _readOffset = offset;
                }
                
            }
        }
        catch (Exception ex)
        {
            await Close();
        }
    }

    private async Task Close()
    {
        _stopped = true;
        _source?.Dispose();
        await Task.WhenAll(_chasers.Select(x => x.Close()).ToArray());
       
        _buffer = null;
        ArrayPool<byte>.Shared.Return(_sharedBuffer);
        Stopped?.Invoke(this, EventArgs.Empty);
    }
}