namespace ModelingEvolution.IO.Nal;

public struct NALUnit
{
    public NALType Type { get; set; }
    public ulong FrameNumber { get; set; }
    public ulong BufferOffset { get; set; }
}