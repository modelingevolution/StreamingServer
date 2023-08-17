using System.Diagnostics;

namespace ModelingEvolution.IO.Tests;

public class ControlStream : Stream
{
	private MemoryStream _ms;
	public MemoryStream Stream => _ms;
	private bool _isLocked;

	/// <summary>
	/// B/ms
	/// </summary>
    private int _readSpeed;
	
    private Stopwatch _sw;
    private bool _readLimitEnabled;

    public int ReadSpeedKBs
    {
        get => _readSpeed * 1000 / 1024;
        private set => _readSpeed = 1024 * value / 1000;
    }

    public void EnableReadLimit(int limit)
    {
        lock (this)
        {
            ReadSpeedKBs = limit;
			_readLimitEnabled = true;
			_sw.Start();
        }
    }
	public void LockRead()
	{
		lock (this)
			_isLocked = true;
		
	}

	public void UnlockRead()
	{
		lock (this)
		{
			_isLocked = false;
			Monitor.Pulse(this);
		}
	}
	public ControlStream()
	{
		_ms = new MemoryStream();
        _sw = new Stopwatch();
        _readSpeed = 30;

    }
	public override void Flush()
	{
		_ms.Flush();
	}

	

	public override int Read(byte[] buffer, int offset, int count)
    {
        long maxCount = Int32.MaxValue;
		lock (this)
        {
            if (_readLimitEnabled)
            {
                maxCount = 0;
                var t = _sw.ElapsedMilliseconds;
                var b = this._readSpeed * t;
                if (b > 0)
                {
                    maxCount = b;
                    _sw.Restart();
                }
            }

            while (_isLocked)
				Monitor.Wait(this);
		}

        var c = Math.Min(maxCount, count);
		return _ms.Read(buffer, offset, (int)c);
	}


	public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = new CancellationToken())
	{
        
       
		return await base.ReadAsync(buffer, cancellationToken);
	}

	public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
        long maxCount = Int32.MaxValue;
        //lock (this)
        //{
        //    if (_readLimitEnabled)
        //    {
        //        maxCount = 0;
        //        var t = _sw.ElapsedMilliseconds;
        //        var b =  this._readSpeed * t;
        //        if (b > 0)
        //        {
        //            maxCount = b;
        //            _sw.Restart();
        //        }
        //    }

        //    while (_isLocked)
        //        Monitor.Wait(this);
        //}
        //var c = Math.Min(maxCount, count);
        return await base.ReadAsync(buffer, offset, count, cancellationToken);
	}

	public override long Seek(long offset, SeekOrigin origin)
	{
		return _ms.Seek(offset, origin);
	}

	public override void SetLength(long value)
	{
		_ms.SetLength(value);
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		_ms.Write(buffer, offset, count);
	}

	public override bool CanRead => _ms.CanRead;

	public override bool CanSeek => _ms.CanSeek;

	public override bool CanWrite => _ms.CanWrite;

	public override long Length => _ms.Length;

	public override long Position
	{
		get => _ms.Position;
		set => _ms.Position = value;
	}
}