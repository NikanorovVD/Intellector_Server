using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IntellectorServer.Core
{
    public static class Networking
    {
        // Send INT
        public static void SendInt(int mes, NetworkStream stream)
        {
            stream.Write(BitConverter.GetBytes(mes), 0, 4);
        }

        public static async Task SendIntAsync(int mes, NetworkStream stream, CancellationToken cancellationToken)
        {
            await stream.WriteAsync(BitConverter.GetBytes(mes).AsMemory(0, 4), cancellationToken);
        }

        public static async Task SendIntAsync(int mes, NetworkStream stream)
            => await SendIntAsync(mes, stream, CancellationToken.None);


        // Receive INT
        static public int RecvInt(NetworkStream stream)
        {
            byte[] recv_bytes = new byte[4];
            stream.ReadExactly(recv_bytes, 0, 4);
            return BitConverter.ToInt32(recv_bytes);
        }

        static public async Task<int> RecvIntAsync(NetworkStream stream, CancellationToken cancellationToken)
        {
            byte[] recv_bytes = new byte[4];
            await stream.ReadExactlyAsync(recv_bytes.AsMemory(0, 4), cancellationToken);
            return BitConverter.ToInt32(recv_bytes);
        }

        static public async Task<int> RecvIntAsync(NetworkStream stream)
            => await RecvIntAsync(stream, CancellationToken.None);


        // Send STRING
        static public void SendString(string str, NetworkStream stream)
        {
            byte[] str_bytes = Encoding.Default.GetBytes(str);
            SendInt(str_bytes.Length, stream);
            stream.Write(str_bytes, 0, str_bytes.Length);
        }

        static public async Task SendStringAsync(string str, NetworkStream stream, CancellationToken cancellationToken)
        {
            byte[] str_bytes = Encoding.Default.GetBytes(str);
            await SendIntAsync(str_bytes.Length, stream, cancellationToken);
            await stream.WriteAsync(str_bytes, cancellationToken);
        }

        static public async Task SendStringAsync(string str, NetworkStream stream)
            => await SendStringAsync(str, stream, CancellationToken.None);


        // Receive STRING
        static public string RecvString(NetworkStream stream)
        {
            int str_len = RecvInt(stream);
            byte[] str_bytes = new byte[str_len];
            stream.ReadExactly(str_bytes, 0, str_len);
            return Encoding.Default.GetString(str_bytes);
        }

        static public async Task<string> RecvStringAsync(NetworkStream stream, CancellationToken cancellationToken)
        {
            int str_len = await RecvIntAsync(stream, cancellationToken);
            byte[] str_bytes = new byte[str_len];
            await stream.ReadExactlyAsync(str_bytes, cancellationToken);
            return Encoding.Default.GetString(str_bytes);
        }

        static public async Task<string> RecvStringAsync(NetworkStream stream)
            => await RecvStringAsync(stream, CancellationToken.None);
    }
}
