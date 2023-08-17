using System;
using System.Collections.ObjectModel;
using ModelingEvolution.IO;

namespace TcpMultiplexer.Smoker.Pages
{
    public class SmokeVm
    {
        public readonly ObservableCollection<SmokeVideoGenerator> Cameras = new();
        public readonly ObservableCollection<SmokeVideoPlayer> Players = new();
        public async Task AddNew(int port)
        {
            Cameras.Add(new SmokeVideoGenerator(port));
        }

        public async Task ConnectClient(string uri, string streamName)
        {
            Players.Add(new SmokeVideoPlayer(new Uri(uri), streamName));
        }
    }
}
