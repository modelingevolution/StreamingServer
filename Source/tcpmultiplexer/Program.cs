using System.Net;
using System.Net.Sockets;
using System.Reflection.PortableExecutable;
using ModelingEvolution.IO;

namespace tcpmultiplexer
{
	internal class Program
	{
		static async Task Main(string[] args)
		{
			Uri source = new Uri(args[0]);
			TcpClient sourceClient = new TcpClient(source.Host, source.Port);
			var sourceStream = sourceClient.GetStream();

			var port = args.Length > 1 ? int.Parse(args[1]):6001;
			Console.Write("Tcp multiplexer, ");
			TcpListener tcp = new TcpListener(IPAddress.Any, port);
			tcp.Start();
			Console.WriteLine($" listening on port: {port}");
			Console.WriteLine($"Source: {source}");

			StreamMultiplexer streamMultiplexer = new StreamMultiplexer(new NonBlockingNetworkStream(sourceStream), null);
			streamMultiplexer.Start();
			Console.WriteLine("Reader started.");

			while (true)
			{
				var client = await tcp.AcceptTcpClientAsync();
				var targetStream = client.GetStream();
				streamMultiplexer.Chase(targetStream);
				Console.WriteLine("Chaser created.");
			}
		}
		
	}
}