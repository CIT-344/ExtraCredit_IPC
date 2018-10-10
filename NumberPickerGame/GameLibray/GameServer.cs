using GameLibray.Enums;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace GameLibray.Server
{
    public delegate void OnStatusChangeHandler(ServerClientReference Client, ConnectionType Connection);
    public class GameServer
    {

        private Task ServerListenerThread;
        private readonly CancellationTokenSource ListenerCancelSource = new CancellationTokenSource();
        private CancellationToken ListenerCancelToken => ListenerCancelSource.Token;

        private readonly TcpListener UnderlyingListener;

        public event OnStatusChangeHandler OnClientStatusChanged;

        private ConcurrentDictionary<Guid, ServerClientReference> ConnectedClients = new ConcurrentDictionary<Guid, ServerClientReference>();

        private readonly AutoResetEvent ServerReadySignal = new AutoResetEvent(false);

        public int NumberToGuess
        {
            get;
            private set;
        }
        

        public GameServer(IPEndPoint BindPoint)
        {
            UnderlyingListener = new TcpListener(BindPoint);
            OnClientStatusChanged += InternalClientEventChange;
        }

        private void InternalClientEventChange(ServerClientReference Client, ConnectionType Connection)
        {
            switch(Connection)
            {
                case ConnectionType.Connected:
                    ConnectedClients.TryAdd(Client.ID, Client);
                    break;
                case ConnectionType.Disconnected:
                    ConnectedClients.TryRemove(Client.ID, out ServerClientReference oldClient);
                    break;

            }
        }

        public IPEndPoint GetBindingInformation()
        {
            if (UnderlyingListener != null)
            {
                return (IPEndPoint)UnderlyingListener.Server.LocalEndPoint;
            }
            else
            {
                return null;
            }
        }

        public void StartServer()
        {
            ServerListenerThread = Task.Run(()=> 
            {
                try
                {
                    UnderlyingListener.Start();
                    ServerReadySignal.Set(); // Tell the StartServer method it can return void now
                    while (!ListenerCancelToken.IsCancellationRequested)
                    {
                        // Block this thread waiting for a client to connect
                        var newClient = UnderlyingListener.AcceptTcpClient();

                        // After a client connects but before processing it
                        // If we are trying to shutdown the server just kill this thread here
                        // Sorry client :p
                        ListenerCancelToken.ThrowIfCancellationRequested();


                        // Send this new client into the Collection of clients and during their constructor they will setup how to handle their stuff
                        // Do this on another thread to make sure that nothing is being blocked
                        SetupNewClient(newClient);
                        
                    }
                }
                catch (OperationCanceledException endThreadRequested)
                {

                }
                catch (Exception e)
                {
                    
                }
                finally
                {
                    StopServer();
                    ServerReadySignal.Set(); // If for whatever reason something goes wrong this will at least tell the method to continue
                }
            }, ListenerCancelToken);

            ServerReadySignal.WaitOne(); // Block this thread until the ServerListener has actually hit the Start line

        }


        /// <summary>
        /// On a new thread setup all the required information
        /// </summary>
        /// <param name="newClient">The TcpClient used for communication</param>
        private void SetupNewClient(TcpClient newClient)
        {
            var serverReference = new ServerClientReference(Guid.NewGuid(), newClient, OnClientStatusChanged, ListenerCancelToken);
            serverReference.OnClientWon += OnWinnerFound;
            OnClientStatusChanged?.Invoke(serverReference, ConnectionType.Connected);
        }

        /// <summary>
        /// Gets called when a client writes a winning number to the server
        /// </summary>
        /// <param name="ClientID">The GUID of the winning connection</param>
        /// <param name="RemoteEndpoint">The IP address and port of the winning connection</param>
        private void OnWinnerFound(Guid ClientID, IPEndPoint RemoteEndpoint)
        {
            // Broadcast to the other clients the game is over!
            foreach (var client in ConnectedClients)
            {
                client.Value.WriteEndGameMessage();
            }

            // Invoke my event that server owner can listen to
            // TODO
        }

        public void StartGame()
        {

            // Safe guards to prevent the server thread from re-firing
            // Maybe other code has already started the server and is just waiting for the game to start!
            if (ServerListenerThread == null)
            {
                StartServer();
            }
            else if (ServerListenerThread != null && !ServerListenerThread.IsCompleted)
            {
                StartServer();
            }

            // Setup things like the random number to guess
            NumberToGuess = new Random().Next(0, 101);
            foreach (var client in ConnectedClients)
            {
                // Tell the clients it's time to PARTY! or pick numbers, whatever they're into
                client.Value.StartGame(NumberToGuess);
            }
        }

        public void StopServer(TimeSpan Delay)
        {
            UnderlyingListener.Stop();
            ListenerCancelSource.CancelAfter(Delay);
            ConnectedClients.Clear();
        }

        public void StopServer()
        {
            // Call the other StopServer with an instant result
            StopServer(TimeSpan.Zero);
        }

        public void StopGame()
        {
            // Tell the clients the game is over but don't kill their connection

        }


    }
}
