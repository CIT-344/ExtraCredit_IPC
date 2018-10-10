using GameLibray.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ClientConsole
{
    class Program
    {
        const int ServerPort = 11000;
        static GameClient ClientEngine = new GameClient();
        static void Main(string[] args)
        {
            Console.WriteLine($"Connecting to server at {IPAddress.Loopback} on port {ServerPort}");
            ClientEngine.ConnectToServer(new IPEndPoint(IPAddress.Loopback, ServerPort));

            var info = ClientEngine.GetBindingInformation();

            if (info != null)
            {
                Console.WriteLine($"Client is connected to {info.Address} on port {info.Port}");
            }
            else
            {
                Console.WriteLine("Error setting up server");
            }


            while (true)
            {

            }
        }
    }
}
