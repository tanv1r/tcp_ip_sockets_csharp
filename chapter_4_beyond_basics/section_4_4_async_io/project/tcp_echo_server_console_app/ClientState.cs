namespace tcp_echo_server_console_app
{
    using System;
    using System.Net.Sockets;

    internal sealed class ClientState
    {
        private const int kBufferSizeByte = 32;
        private byte[] receiveBuffer;
        private Socket clientSocket;

        public ClientState(Socket socket)
        {
            this.clientSocket = socket;
            receiveBuffer = new byte[kBufferSizeByte];
        }

        public byte[] ReceiveBuffer => this.receiveBuffer;

        public Socket ClientSocket => this.clientSocket;
    }
}