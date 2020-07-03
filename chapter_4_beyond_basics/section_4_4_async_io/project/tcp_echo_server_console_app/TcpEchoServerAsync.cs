namespace tcp_echo_server_console_app
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;

    public sealed class TcpEchoServerAsync
    {
        private const int kDoSomethingElseLoopCount = 5;
        private const int kDoSomethingElseLoopSleepSec = 1;

        //  Outstanding TCP connection queue maximum size
        private const int kBackLogCount = 5;

        private static int ReadThreadId => Thread.CurrentThread.GetHashCode();

        private static ThreadState ReadThreadState => Thread.CurrentThread.ThreadState;

        public static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                throw new ArgumentException($"Parameters: <Port>");
            }

            int serverPort = int.Parse(args[0]);

            // Create socket to accept client connections
            Socket serverSocket = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp);

            serverSocket.Bind(
                new IPEndPoint(
                    IPAddress.Any,
                    serverPort));
            serverSocket.Listen(kBackLogCount);

            while (true)
            {
                Console.WriteLine($"Thread {ReadThreadId} ({ReadThreadState}) - Main(): calling BeginAccept()");

                IAsyncResult future =
                    serverSocket.BeginAccept(
                        new AsyncCallback(AcceptCallback),
                        state: serverSocket);

                DoSomethingElse();

                future.AsyncWaitHandle.WaitOne();
            }
        }

        public static void DoSomethingElse()
        {
            for (int i = 1; i <= kDoSomethingElseLoopCount; ++i)
            {
                Console.WriteLine($"Thread {ReadThreadId} ({ReadThreadState} - DoSomethingElse(): {i}...");

                Thread.Sleep(kDoSomethingElseLoopSleepSec*1000);
            }
        }

        public static void AcceptCallback(IAsyncResult future)
        {
            Socket serverSocket = future.AsyncState as Socket;
            Socket clientSocket = null;

            try
            {
                // After this call, future.AsyncWaitHandle.WaitOne() will unblock to accept more client connection
                clientSocket = serverSocket.EndAccept(future);

                Console.WriteLine($"Thread {ReadThreadId} ({ReadThreadState}) - AcceptCallback(): handling client at {clientSocket.RemoteEndPoint}");

                // We have all necessary information to receive from client connection
                // Prepare for BeginReceive
                //
                ClientState cs = new ClientState(clientSocket);

                clientSocket.BeginReceive(
                    cs.ReceiveBuffer,
                    0,
                    cs.ReceiveBuffer.Length,
                    SocketFlags.None,
                    new AsyncCallback(ReceiveCallback),
                    cs
                );
            }
            catch (SocketException se)
            {
                Console.WriteLine($"{se.ErrorCode}: {se.Message}");
                clientSocket.Close();
            }
        }

        public static void ReceiveCallback(IAsyncResult future)
        {
            ClientState cs = future.AsyncState as ClientState;

            try
            {
                // We are looping with repeated EndReceive (in ReceiveCallback) and BeginReceive (in SendCallback)
                // TCP preserves message sequence, that is how the client can consolidate all chunks and print
                // the whole message.
                //
                int receivedMessageSizeByte = cs.ClientSocket.EndReceive(future);

                if (receivedMessageSizeByte > 0)
                {
                    Console.WriteLine($"Thread {ReadThreadId} ({ReadThreadState}) - ReceiveCallback(): received {receivedMessageSizeByte} bytes");

                    // Remember, this is an echo server, so send back whatever was received.
                    cs.ClientSocket.BeginSend(
                        cs.ReceiveBuffer,
                        0,
                        receivedMessageSizeByte,
                        SocketFlags.None,
                        new AsyncCallback(SendCallback),
                        cs
                    );
                }
                else
                {
                    cs.ClientSocket.Close();
                }
            }
            catch (SocketException se)
            {
                Console.WriteLine($"{se.ErrorCode}: {se.Message}");
                cs.ClientSocket.Close();
            }
        }

        public static void SendCallback(IAsyncResult future)
        {
            ClientState cs = future.AsyncState as ClientState;

            try
            {
                int bytesSent = cs.ClientSocket.EndSend(future);

                Console.WriteLine($"Thread {ReadThreadId} ({ReadThreadState}) - SendCallback(): sent {bytesSent} bytes");

                cs.ClientSocket.BeginReceive(
                    cs.ReceiveBuffer,
                    0,
                    cs.ReceiveBuffer.Length,
                    SocketFlags.None,
                    new AsyncCallback(ReceiveCallback),
                    cs
                );
            }
            catch (SocketException se)
            {
                Console.WriteLine($"{se.ErrorCode}: {se.Message}");
                cs.ClientSocket.Close();
            }
        }
    }
}