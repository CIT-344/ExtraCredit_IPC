using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GameLibray.Client
{
    public class GameClient
    {
        private readonly TcpClient UnderlyingConnection;

        private NetworkStream UnderlyingConnectionStream;
        private Task ReaderThread;

        internal StreamReader Reader;

        private readonly AutoResetEvent ClientReadySignal = new AutoResetEvent(false);

        public GameClient()
        {
            UnderlyingConnection = new TcpClient();
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

        public void ConnectToServer(String Host, int Port)
        {
            try
            {
                ConnectToServer(new IPEndPoint(IPAddress.Parse(Host), Port));
            }
            catch (Exception)
            {
                // Pass this exception back to the end-user
                // Most likely to get something like Invalid IP address
                throw;
            }
        }

        public void ConnectToServer(IPEndPoint RemoteEndpoint)
        {
            try
            {
                UnderlyingConnection.Connect(RemoteEndpoint);
                UnderlyingConnectionStream = UnderlyingConnection.GetStream();
                // Do some other setup work!
                StartReader();
                ClientReadySignal.WaitOne();
            }
            catch (Exception)
            {
                // More complicated error something about actually creating the connection to the server
                throw;
            }
        }

        public void Disconnect()
        {
            if (UnderlyingConnection != null)
            {
                UnderlyingConnection.Close();
                if (ReaderThread != null)
                {
                    ReaderThread.Wait(TimeSpan.FromMilliseconds(250)); // Wait for the reader to close or if it takes longer than 250ms just continue
                }
            }
        }


        private void StartReader()
        {
            ReaderThread = Task.Factory.StartNew(() =>
            {
                try
                {
                    Reader = new StreamReader(UnderlyingConnectionStream, Encoding.UTF8);
                    ClientReadySignal.Set();
                    while (UnderlyingConnection != null && UnderlyingConnection.Connected)
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
            },TaskCreationOptions.LongRunning);
            
        }
    }
}
