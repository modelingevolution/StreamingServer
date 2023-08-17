using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace ModelingEvolution.IO
{
	public static class StreamExtensions
	{
		static readonly string[] SizeSuffixes =
			{ "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
		static string SizeSuffix(Int64 value, int decimalPlaces = 1)
		{
			if (decimalPlaces < 0) { throw new ArgumentOutOfRangeException("decimalPlaces"); }
			if (value < 0) { return "-" + SizeSuffix(-value, decimalPlaces); }
			if (value == 0) { return string.Format("{0:n" + decimalPlaces + "} bytes", 0); }

			// mag is 0 for bytes, 1 for KB, 2, for MB, etc.
			int mag = (int)Math.Log(value, 1024);

			// 1L << (mag * 10) == 2 ^ (10 * mag) 
			// [i.e. the number of bytes in the unit corresponding to mag]
			decimal adjustedSize = (decimal)value / (1L << (mag * 10));

			// make adjustment when the value is large enough that
			// it would round up to 1000 or more
			if (Math.Round(adjustedSize, decimalPlaces) >= 1000)
			{
				mag += 1;
				adjustedSize /= 1024;
			}

			return string.Format("{0:n" + decimalPlaces + "} {1}",
				adjustedSize,
				SizeSuffixes[mag]);
		}
		public static async Task CopyToAsync(this Stream source, 
			Stream destination, int bufferSize=4*1024, int sleepMiliseconds = 0)
		{
			byte[] buffer = new byte[bufferSize/2];
			byte[] buffer2 = new byte[bufferSize/2];

			var c = await source.ReadAsync(buffer, 0, buffer.Length);
			var task = source.ReadAsync(buffer2, 0, buffer2.Length);

			long total = 0;
			var stopWatch = new Stopwatch();
			stopWatch.Start();
			var totalStopWatch = new Stopwatch();
			totalStopWatch.Start();
			while (c > 0)
			{
				await destination.WriteAsync(buffer, 0, c);
				c = await task;

				// swapping buffers
				var n = buffer;
				buffer = buffer2;
				buffer = n;

				task = source.ReadAsync(buffer2, 0, buffer2.Length);
				
				total += c;

				if (stopWatch.ElapsedMilliseconds > 500)
				{
					var size = SizeSuffix(total);
					Console.Write($"\rBytes streamed: {size}, {total/totalStopWatch.Elapsed.TotalSeconds}");
					stopWatch.Restart();
				}
				if(sleepMiliseconds > 0)
					await Task.Delay(sleepMiliseconds);
			}

			Console.WriteLine();
			Console.WriteLine("Completed");
		}
	}
}