using Microsoft.VisualBasic;

namespace ModelingEvolution.IO.Nal;

public sealed class ReverseDecoder : IDecoder
{
    public event EventHandler<NALUnit> FrameDecoded;
    // Moore automat
    private Func<byte, uint, NALType?> _currentState;
    private ulong _number;

    public ReverseDecoder()
    {
        _currentState = Decode_State1;
    }
    public void Decode(byte[] data, int read)
    {
        for (uint i = 0; i < read; i++)
        {
            Decode(data[i], i);
        }

    }

    public NALType? Decode(byte b)
    {
        return Decode(b, 0);
    }

    private NALType _type;
    private NALType? Decode_State1(byte b, uint i)
    {
        var nalType = (NALType)b;
        switch (nalType)
        {
            case NALType.ARP:
            case NALType.PPS:
            case NALType.IDRBFrame:
            case NALType.NIDRBFrame:
            case NALType.IFrame:
            case NALType.SPS:
            case NALType.PFrame:
                _type = nalType;
                _currentState = Decode_State2;
                break;
            default:
                break;
        }

        return null;
    }
    
    private NALType? Decode_State2(byte b, uint i)
    {
        if (b == 0x01)
        {
            _currentState = Decode_State3;
            return null;
        }

        _currentState = Decode_State1;
        return null;
    }

    private NALType? Decode_State3(byte b, uint i)
    {
        if (b == 0x0)
        {
            _currentState = Decode_State4;
            return null;
        }

        _currentState = Decode_State1;
        return null;
    }
    private NALType? Decode_State4(byte b, uint i)
    {
        if (b == 0x0)
        {
            _currentState = Decode_State5;
            return null;
        }

        _currentState = Decode_State1;
        return null;
    }
    private NALType? Decode_State5(byte b, uint i)
    {
        if (b == 0x0)
        {
            FrameDecoded?.Invoke(this, new NALUnit()
            {
                FrameNumber = _number++,
                Type = _type,
                BufferOffset = i
            });
            _currentState = Decode_State1;
            return _type;
        }

        _currentState = Decode_State1;
        return null;
    }
    private NALType? Decode(byte b, uint i)
    {
        return _currentState(b, i);
    }
}