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
        static GameServer GameEngine;
        static void Main(string[] args)
        {
            Console.WriteLine("Server Starting . . . .");

            GameEngine = new GameServer(new IPEndPoint(IPAddress.Loopback, 11000));
            GameEngine.OnClientConnected += LogClientConnected;



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



            while (GameEngine != null)
            {
                // Never end the program!
            }
        }

        private static void LogClientConnected(ServerClientReference NewClient)
        {
            var info = NewClient.GetBindingInformation();
            Console.WriteLine($"A new client has joined the server at {info.Address} on remote port {info.Port}");
        }
    }
}
