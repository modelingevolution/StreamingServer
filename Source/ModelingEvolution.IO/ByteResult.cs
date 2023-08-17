namespace ModelingEvolution.IO;

public readonly struct ByteResult
{
    private readonly int _value;
    public static readonly ByteResult NaN = new ByteResult(int.MaxValue);
    public bool IsNaN => _value == int.MaxValue;
    private ByteResult(int value)
    {
        _value = value;
    }

    public static implicit operator byte(ByteResult r)
    {
        if (r._value == int.MaxValue)
            throw new InvalidOperationException("NaN");
        return (byte)r._value;
    }

    public static implicit operator ByteResult(int value)
    {
        return new ByteResult(value);
    }
}