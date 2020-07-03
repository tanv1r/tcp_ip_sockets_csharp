using System.Net.Sockets;
using System.Text;

namespace tcp_echo_client_console_app
{
    // This is the state that would be passed into Begin* call.
    // IAsyncResult would also be of type ClientState, but we need to cast before accessing it
    // inside the callback.
    //
    public class ClientState
    {
        private byte[] byteBuffer;
        private NetworkStream networkStream;
        private StringBuilder echoResponse;
        private int totalBytesReceived = 0;

        public ClientState(NetworkStream networkStream, byte[] byteBuffer)
        {
            this.networkStream = networkStream;
            this.byteBuffer = byteBuffer;
            this.echoResponse = new StringBuilder();
        }

        public NetworkStream NetworkStream => this.networkStream;

        public byte[] ByteBuffer
        {
            get
            {
                return this.byteBuffer;
            }

            set
            {
                this.byteBuffer = value;
            }
        }

        public void AppendResponse(string response)
        {
            this.echoResponse.Append(response);
        }

        public string EchoResponse => this.echoResponse.ToString();

        public void AddToTotalBytes(int count)
        {
            this.totalBytesReceived += count;
        }

        public int TotalBytes => this.totalBytesReceived;
    }
}