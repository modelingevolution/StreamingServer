using System.Reflection.Metadata.Ecma335;

namespace ModelingEvolution.IO.Nal;

public sealed class Decoder : IDecoder
{
    public event EventHandler<NALUnit> FrameDecoded;
    private ulong _zCount = 0;
    private ulong _number = 0;
    private byte _prv = 0;
    public void Decode(byte[] data, int read)
    {
        for (uint i = 0; i < read; i++) {
            Decode(data[i], i);
        }

    }

    public NALType? Decode(byte b)
    {
        return Decode(b,0);
    }

    private NALType? Decode(byte b, uint i)
    {
        if (b == 0) _zCount += 1;
        else if (_zCount == 3)
        {
            if (b == 1)
            {
                _prv = 1;
                return null;
            }

            _zCount = 0;

            if (_prv != 1)
            {
                _prv = 0;
                return null;
            }

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
                    FrameDecoded?.Invoke(this, new NALUnit()
                    {
                        FrameNumber = _number++,
                        Type = nalType,
                        BufferOffset = i
                    });
                    break;
                default:
                    throw new InvalidOperationException("WTF");
            }

            return nalType;
        }
        else
        {
            _zCount = 0;
        }

        return null;
    }
}