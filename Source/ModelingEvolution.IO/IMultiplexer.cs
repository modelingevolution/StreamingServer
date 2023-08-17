namespace ModelingEvolution.IO;

interface IMultiplexer
{
    Memory<byte> Buffer();
    int ReadOffset { get; }
    ulong TotalReadBytes { get; }
    void Disconnect(IChaser chaser);
}