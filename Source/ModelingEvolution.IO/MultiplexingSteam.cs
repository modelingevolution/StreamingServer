namespace ModelingEvolution.IO;

public class BinaryStream : StreamBase
{
    private readonly Stream _stream;

    public BinaryStream(Stream stream)
    {
        _stream = stream;
    }

    protected override void Dispose(bool disposing)
    {
        _stream.Dispose();
    }

    public override ValueTask<int> ReadAsync(Memory<byte> buffer)
    {
        return _stream.ReadAsync(buffer);
    }
}