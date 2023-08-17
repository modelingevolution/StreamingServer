using System.Diagnostics;
using System.Runtime.CompilerServices;
using Castle.Core.Logging;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using ModelingEvolution.IO.Nal;
using NSubstitute;
using NSubstitute.Core;

namespace ModelingEvolution.IO.Tests
{
    public class StreamMultiplexerTests
	{
		


        [Fact]
        public async Task ReverseDetectionWorks()
        {

            IDecoder d = new ReverseDecoder();
            DecoderStats stats = new DecoderStats();
            stats.Wire(d);

            var bufferStream = new MemoryStream();
            var len = bufferStream.WriteRandomData(128 * 1024, new byte[] { 0x00, 0x00, 0x00, 0x01, (byte)NALType.SPS }, 1024);
            var bufferArray = bufferStream.GetBuffer();

            for (int i = (int)len-1; i >= 0; i--)
            {
                var b = bufferArray[i];
                d.Decode(b);
            }
            

            stats.Sps.Should().Be(127);
        }

        [Fact]
        public async Task DetectionWorks()
        {

            Decoder d = new Decoder();
            DecoderStats stats = new DecoderStats();
			stats.Wire(d);

            var bufferStream = new ControlStream();
            bufferStream.WriteRandomData(128 * 1024, new byte[] { 0x00, 0x00, 0x00, 0x01, (byte)NALType.SPS }, 1024);
            bufferStream.Position = 0;

            var read = 1;
            byte[] buffer = new byte[1024];
            while ((read = await bufferStream.ReadAsync(buffer)) > 0)
            {
				d.Decode(buffer, read);
            }

            stats.Sps.Should().Be(127);
        }

		[Fact]
		public async Task Works()
        {
            Decoder d = new Decoder();
			var ms = new ControlStream();
			ms.WriteRandomData(128 * 1024, new byte[] { 0x00, 0x00, 0x00, 0x01, (byte)NALType.SPS }, 1024);
			ms.Position = 0;
			
			ms.EnableReadLimit(30);

			StreamMultiplexer m = new StreamMultiplexer(new BinaryStream(ms), Substitute.For<ILogger<StreamMultiplexer>>());
			m.Start();
			await Task.Delay(1000);
			MemoryStream dst = new MemoryStream();
			m.Chase(dst, x => d.Decode(x) == NALType.SPS ? -4 : null);

			await Task.Delay(1000);
			dst.Length.Should().Be(StreamMultiplexer.CHUNK);
			long start = ms.Length - dst.Length;

			ms.LockRead();
			var position = ms.Position;
			ms.WriteRandomData(1024 * 1024*10, new byte[] { 0x00, 0x00, 0x00, 0x01, (byte)NALType.SPS }, 1024);
			ms.Position = position;
			ms.UnlockRead();

			await Task.Delay(1000);

			var src = ms.Stream.GetBuffer();
			var read = dst.GetBuffer();

			int j = 0;
			for (var i = start; i < ms.Length; i++)
			{
				src[i].Should().Be(read[j++]);
			}

		}
	}
}