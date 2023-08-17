using System.Net.Sockets;
using ModelingEvolution.IO;

namespace tcpdumper
{
	internal class Program
	{
		static async Task Main(string[] args)
		{
			try
			{
				Uri u = new Uri(args[0]);
				string fileName = args[1];
				await Task.Delay(5000);
				Console.WriteLine($"Reading from {u}");

				TcpClient client = new TcpClient(u.Host, u.Port);
				var stream = client.GetStream();
				using Stream o = fileName == "null" ? Stream.Null : new FileStream(fileName, FileMode.Create, FileAccess.Write);
				await StreamExtensions.CopyToAsync(stream, o, 16*1024);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine(ex.Message);
			}
		}
	}
}