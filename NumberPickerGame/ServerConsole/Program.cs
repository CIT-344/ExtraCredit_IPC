using GameLibray.Enums;
using GameLibray.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ServerConsole
{
    class Program
    {
        const int MinPlayerCount = 1;

        static GameServer GameEngine;
        static void Main(string[] args)
        {
            Console.WriteLine("Server Starting . . . .");

            GameEngine = new GameServer(new IPEndPoint(IPAddress.Loopback, 11000));
            GameEngine.OnClientStatusChanged += LogClientConnectionChanged;



            GameEngine.StartServer();
            var info = GameEngine.GetBindingInformation();

            if (info != null)
            {
                Console.WriteLine($"Server is running at {info.Address} on port {info.Port}");
            }
            else
            {
                Console.WriteLine("Error setting up server");
            }


            do
            {
                // Not enough clients take a nap until they all connect
                Task.Delay(TimeSpan.FromSeconds(1)).Wait();


            } while (GameEngine.GetClientCount() < MinPlayerCount);

            // Time to play!
            GameEngine.StartGame();

            while (GameEngine != null)
            {
                // Never end the program!
            }
        }

        private static void LogClientConnectionChanged(ServerClientReference Client, ConnectionType ConnectionStatus)
        {

            switch (ConnectionStatus)
            {
                case ConnectionType.Connected:
                    {
                        var info = Client.GetBindingInformation();
                        if (info != null)
                        {
                            Console.WriteLine($"A new client has joined the server at {info.Address} on remote port {info.Port}");
                        }
                        break;
                    }

                case ConnectionType.Disconnected:
                    Console.WriteLine($"Client {Client.ID} has left the server");
                    break;
            }
        }
    }
}
