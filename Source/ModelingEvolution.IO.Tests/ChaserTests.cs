using FluentAssertions;
using FluentAssertions.Execution;
using ModelingEvolution.IO.Nal;
using NSubstitute;

namespace ModelingEvolution.IO.Tests;

public class ChaserTests
{
    [Fact]
    public void EdgeOfStream()
    {
        byte[] buffer = new byte[4 * 1024];
        buffer.WriteRandomData(0, 2);
        buffer.WriteVideoKeyFrame(1024);

        IMultiplexer m = Substitute.For<IMultiplexer>();
        m.Buffer().Returns(buffer);
        m.ReadOffset.Returns(2); // we are at the beginning.
        m.TotalReadBytes.Returns((ulong)(buffer.Length + 2));

        MemoryStream dst = new MemoryStream();
        IDecoder d = new ReverseDecoder();

        Chaser sut = new Chaser(m, dst, x => d.Decode(x) == NALType.SPS ? 0 : null);
        sut.Start();

        Thread.Sleep(1000);

        var actual = dst.GetBuffer().AsMemory(0, (int)sut.WrittenBytes);
        var first = buffer.AsMemory(1024, 3 * 1024);
        var last = buffer.AsMemory(0, 2);

        actual.Slice(0,3*1024).ShouldBe(first);
        actual.Slice(3*1024,2).ShouldBe(last);

        sut.Cancel();
    }

    [Fact]
    public void MidOfStream()
    {
        byte[] buffer = new byte[4 * 1024];
        var offset = 1024;
        offset += buffer.WriteVideoKeyFrame(1024);
        offset += buffer.WriteRandomData(offset, 1024);

        IMultiplexer m = Substitute.For<IMultiplexer>();
        m.Buffer().Returns(buffer);
        m.ReadOffset.Returns(offset);
        m.TotalReadBytes.Returns((ulong)offset);

        MemoryStream dst = new MemoryStream();
        IDecoder d = new ReverseDecoder();

        Chaser sut = new Chaser(m, dst, x => d.Decode(x) == NALType.SPS ? 0 : null);
        sut.Start();

        Thread.Sleep(1000);

        var actual = dst.GetBuffer().AsMemory(0, (int)sut.WrittenBytes);
        actual.ShouldBe(buffer.AsMemory(1024, offset-1024));

        sut.Cancel();
    }

    [Fact]
    public void StartOfStream()
    {
        byte[] buffer = new byte[4 * 1024];
        var offset = buffer.WriteVideoKeyFrame();
        offset += buffer.WriteRandomData(offset, 1024);

        IMultiplexer m = Substitute.For<IMultiplexer>();
        m.Buffer().Returns(buffer);
        m.ReadOffset.Returns(offset);
        m.TotalReadBytes.Returns((ulong)offset);


        MemoryStream dst = new MemoryStream();
        IDecoder d = new ReverseDecoder();

        Chaser sut = new Chaser(m, dst, x => d.Decode(x) == NALType.SPS ? 0 : null);
        sut.Start();

        Thread.Sleep(1000);

        var actual = dst.GetBuffer().AsMemory(0, (int)sut.WrittenBytes);
        actual.ShouldBe(buffer.AsMemory(0,offset));

        sut.Cancel();
    }
}
public static class MemoryAssertions {
    public static void ShouldBe(this Memory<byte> actual, Memory<byte> expected)
    {
        if (actual.Length != expected.Length)
            throw new AssertionFailedException($"Buffer lengths are different. Should be {expected.Length} but was {actual.Length}.");
        for (int i = 0; i < actual.Length; i++)
        {
            if (actual.Span[i] != expected.Span[i])
            {
                throw new AssertionFailedException($"Buffers are different at {i}, {actual.Span[i]} should be {expected.Span[i]}");
            }
        }
    }
}