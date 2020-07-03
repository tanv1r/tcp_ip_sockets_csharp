using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace tcp_echo_client_console_app
{
    public class TcpEchoClientAsync
    {
        private const int DoSomethingElseLoopCount = 5;
        private const int DoSomethingElseLoopSleepSec = 1;

        public static int ReadThreadId => Thread.CurrentThread.GetHashCode();

        public static ThreadState ReadThreadState => Thread.CurrentThread.ThreadState;

        // Once it makes the transition false -> true, someone must initiate
        // the transition true -> false manually
        //
        public static ManualResetEvent ReadDone = new ManualResetEvent(false);

        public static void Main(string[] args)
        {
            if (args.Length != 2 && args.Length != 3)
            {
                throw new ArgumentException($"Parameters: <Server> <Word> [<Port>]");
            }

            // Server name or ip address
            string server = args[0];

            int serverPort = (args.Length == 3) ? int.Parse(args[2]) : 7;

            Console.WriteLine($"Thread {ReadThreadId} ({ReadThreadId}) - Main()");

            TcpClient client = new TcpClient();

            client.Connect(server, serverPort);

            Console.WriteLine($"Thread {ReadThreadId} ({ReadThreadState}) - Main(): connected to server");

            NetworkStream networkStream = client.GetStream();

            ClientState cs = new ClientState(networkStream, Encoding.ASCII.GetBytes(args[1]));

            // Asynchronously send the message as ascii encoded bytes to the server
            IAsyncResult future = networkStream.BeginWrite(
                cs.ByteBuffer,
                0,
                cs.ByteBuffer.Length,
                new AsyncCallback(WriteCallback),
                cs
            );

            DoSomethingElse();

            // Block until EndWrite is called inside the callback
            future.AsyncWaitHandle.WaitOne();

            future = networkStream.BeginRead(
                cs.ByteBuffer,
                cs.TotalBytes,
                cs.ByteBuffer.Length - cs.TotalBytes,
                new AsyncCallback(ReadCallback),
                cs
            );
        }

        // Is this callback being executed on the thread that did BeginRead
        // or the thread that was executing Read?
        //
        public static void ReadCallback(IAsyncResult future)
        {
            ClientState cs = future.AsyncState as ClientState;

            // We used ReadDone, a ManualResetEvent, so that we can
            // decide when to unblock the waiter, because a single
            // EndRead call may not get all bytes.
            // If, instead, waiter waited with future.AsyncWaitHandler.WaitOne(),
            // he would have been unblocked after the first EndRead call.
            //
            int bytesReceived = cs.NetworkStream.EndRead(future);

            cs.AddToTotalBytes(bytesReceived);
            cs.AppendResponse(Encoding.ASCII.GetString(cs.ByteBuffer, 0, bytesReceived));

            // Until all bytes read, keep calling BeginRead with updated offset
            if (cs.TotalBytes < cs.ByteBuffer.Length)
            {
                Console.WriteLine($"Thread {ReadThreadId} ({ReadThreadState}) - ReadCallback(): Received {bytesReceived}...");
                cs.NetworkStream.BeginRead(
                    cs.ByteBuffer,
                    cs.TotalBytes, // offset
                    cs.ByteBuffer.Length - cs.TotalBytes,
                    new AsyncCallback(ReadCallback),
                    cs
                );
            }
            else
            {
                Console.WriteLine($"Thread {ReadThreadId} ({ReadThreadState}) - ReadCallback(): Received {cs.TotalBytes} total bytes: {cs.EchoResponse}");

                // Signal the end of read
                ReadDone.Set();
            }
        }

        public static void WriteCallback(IAsyncResult future)
        {
            ClientState cs = future.AsyncState as ClientState;

            // After this call to EndWrite, future.AsyncWaitHandle.WaitOne() unblocks
            cs.NetworkStream.EndWrite(future);

            Console.WriteLine($"Thread {ReadThreadId} ({ReadThreadState}) - WriteCallback(): Sent {cs.ByteBuffer.Length} bytes...");
        }

        public static void DoSomethingElse()
        {
            for (int x = 1; x <= DoSomethingElseLoopCount; ++x)
            {
                Console.WriteLine($"Thread {ReadThreadId} ({ReadThreadState}) - DoSomethingElse(): {x}...");
                Thread.Sleep(DoSomethingElseLoopSleepSec*1000);
            }
        }
    }
}