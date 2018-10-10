using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GameLibray.Server
{
    public delegate void ClientGuessedNumberHandler(Guid ClientID, IPEndPoint RemoteEndpoint);
    public class ServerClientReference
    {
        internal int NumberToGuess;
        public event ClientGuessedNumberHandler OnClientWon;

        private readonly TcpClient UnderlyingConnection;
        public readonly Guid ID;

        private Task ReaderThread;

        internal StreamReader Reader;
        private StreamWriter Writer;

        private NetworkStream UnderlyingConnectionStream;

        CancellationToken EndRequest;

        public ServerClientReference(Guid ID, TcpClient Connection, CancellationToken EndRequest)
        {
            this.ID = ID;
            UnderlyingConnection = Connection;
            this.EndRequest = EndRequest;
            UnderlyingConnectionStream = Connection.GetStream();
            WriteGameOptions();
        }

        internal void StartGame(int NumberToGuess)
        {
            this.NumberToGuess = NumberToGuess;
            StartReader();
            WriteGameStart();
            // Send the client some data about the game!
            //Write("Something!");
        }

        public IPEndPoint GetBindingInformation()
        {
            if (UnderlyingConnection != null)
            {
                return (IPEndPoint)UnderlyingConnection.Client.RemoteEndPoint;
            }
            else
            {
                return null;
            }
        }

        private void StartReader()
        {
            ReaderThread = Task.Factory.StartNew(()=> 
            {
                try
                {
                    Reader = new StreamReader(UnderlyingConnectionStream, Encoding.UTF8);
                    while (!EndRequest.IsCancellationRequested)
                    {
                        // TODO - Write Extension to handle directly reading a data object
                        var result = Reader.ReadLine();

                        // Data that comes in here is the client telling the server it's number guess!
                    }
                }
                catch (OperationCanceledException endThread)
                { }
                catch (Exception e)
                {

                }
                finally
                {
                    Reader.Dispose();
                }
            }, EndRequest, TaskCreationOptions.LongRunning, TaskScheduler.Current); // The token connected to ending this thread, a hint to the scheduler that this thread shouldn't come from the pool, the scheduler responsible for creating the thread
        }

        /// <summary>
        /// Sends a message to the underlying stream to transmit data to the client
        /// </summary>
        private void Write()
        {
            // Cleanly create a new writer every time one is needed this prevent any memory leaks on the writer at least
            using (var Writer = new StreamWriter(UnderlyingConnectionStream, Encoding.UTF8) {AutoFlush = true })
            {
                // Will do something like parse my data object
                // But that is later!

                // This writer is the server telling the client something
                Writer.WriteLine();
            }
        }

        /// <summary>
        /// Transmits information to the client about the server, and information regarding the client side reference
        /// </summary>
        internal void WriteGameOptions()
        {
            // Send information like current ID
            // System Time
            // Range of numbers to guess from
            // Etc.
            Write();
        }

        /// <summary>
        /// Transmits to the client the game has ended and who the winner was
        /// </summary>
        internal void WriteEndGameMessage()
        {
            // Send information about the winner to this client
            // If they're lucky this client was the winner!
            Write();
        }


        /// <summary>
        /// Transmit the GO flag to this client indicating that they can begin trying to guess the hidden number
        /// </summary>
        internal void WriteGameStart()
        {
            // Send a go command to inform the client that guessing will now be accepted.
            Write();
        }
    }
}
