namespace ModelingEvolution.IO.Nal;

public class DecoderStats
{
    private long _iframe;
    private long _pframe;
    private long _sps;
    private long _pps;
    private long _idrb;
    private long _nirdb;
    private long _arp;

    public long Iframe => _iframe;

    public long Pframe => _pframe;

    public long Sps => _sps;

    public long Pps => _pps;

    public long Idrb => _idrb;

    public long Nirdb => _nirdb;

    public long Arp => _arp;

    public DecoderStats()
    {

    }

    public void Wire(IDecoder d)
    {
        d.FrameDecoded += OnFrameDecoded;
    }

    private void OnFrameDecoded(object? sender, NALUnit e)
    {
        switch (e.Type)
        {
            case NALType.IFrame:
                this._iframe += 1;
                break;
            case NALType.PFrame:
                this._pframe += 1;
                break;
            case NALType.SPS:
                this._sps += 1;
                break;
            case NALType.PPS:
                this._pps += 1;
                break;
            case NALType.IDRBFrame:
                this._idrb += 1;
                break;
            case NALType.NIDRBFrame:
                this._nirdb += 1;
                break;
            case NALType.ARP:
                this._arp += 1;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}