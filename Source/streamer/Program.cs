using System.Net;
using System.Net.Sockets;
using ModelingEvolution.IO;

namespace StreamerCli
{
	internal class Program
	{
		private static int DELAY = 0;
		static async Task Main(string[] args)
		{
			var port = int.Parse(args[0]);
			Console.WriteLine("File streamer, listening on port: " + port);
			if (args.Length >= 1)
				DELAY = int.Parse(args[1]);
			TcpListener tcp = new TcpListener(IPAddress.Any, port);
			
			tcp.Start();
			while (true)
			{
				try
				{
					var client = await tcp.AcceptTcpClientAsync();
					Thread t = new Thread(Run);
					t.IsBackground = true;
					t.Start(client);
				}
				catch (Exception ex)
				{
					Console.WriteLine("Client disconnected. " + ex.Message);
				}
			}
		}

		static void Run(object client)
		{
			var task = RunAsync((TcpClient)client);
			task.GetAwaiter().GetResult();
		}

		static async Task RunAsync(TcpClient client)
		{
			Console.WriteLine("Streaming started:");
			using NetworkStream stream = client.GetStream();
			using FileStream fs = new FileStream("C:\\Users\\rafal\\Downloads\\BG.mp4", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

			await fs.CopyToAsync(stream, sleepMiliseconds: DELAY);
		}
	}
}