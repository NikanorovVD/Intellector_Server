using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace IntellectorServer
{
    public static class Networking
    {
        public static void SendCode(byte mes, NetworkStream stream)
        {
            byte[] send_bytes = new byte[1] { mes };
            stream.Write(send_bytes, 0, 1);
        }
        public static byte RecvCode(NetworkStream stream)
        {
            byte[] recv_bytes = new byte[1];
            stream.Read(recv_bytes, 0, 1);
            return recv_bytes[0];
        }


        public static void SendMove(byte[] move, NetworkStream stream)
        {
            const byte move_code = 10;
            SendCode(move_code, stream);
            stream.Write(move, 0, 5);
        }
        static public byte[] RecvMove(NetworkStream stream)
        {
            byte[] move_bytes = new byte[5];
            stream.Read(move_bytes, 0, 5);
            return move_bytes;
        }


        public static void SendInt(int mes, NetworkStream stream)
        {
            stream.Write(BitConverter.GetBytes(mes), 0, 4);
        }
        public static void SendUInt(uint mes, NetworkStream stream)
        {
            stream.Write(BitConverter.GetBytes(mes), 0, 4);
        }
        static public int RecvInt(NetworkStream stream)
        {
            byte[] recv_bytes = new byte[4];
            stream.Read(recv_bytes, 0, 4);
            return BitConverter.ToInt32(recv_bytes);
        }

        public static void SendTime(int time, NetworkStream stream)
        {
            const byte time_code = 20;
            SendCode(time_code, stream);
            SendInt(time, stream);
        }


        static public void SendString(string str, NetworkStream stream)
        {
            byte[] str_bytes = Encoding.Default.GetBytes(str);
            SendInt(str_bytes.Length, stream);
            stream.Write(str_bytes, 0, str_bytes.Length);
        }
        static public string RecvString(NetworkStream stream)
        {
            int str_len = RecvInt(stream);
            byte[] str_bytes = new byte[str_len];
            stream.Read(str_bytes, 0, str_len);
            return Encoding.Default.GetString(str_bytes);
        }


        static public void SendGameInfo(GameInfo game, NetworkStream stream)
        {
            SendUInt(game.ID, stream);
            SendString(game.Name, stream);
            SendInt(game.TimeContol.MaxMinutes, stream);
            SendInt(game.TimeContol.AddedSeconds, stream);
            SendInt((int)game.ColorChoice, stream);
        }
        static public GameInfo RecvGameInfo(NetworkStream stream)
        {
            string name = RecvString(stream);
            int max_time = RecvInt(stream);
            int add_time = RecvInt(stream);
            ColorChoice color = (ColorChoice)RecvInt(stream);
            return new GameInfo { Name = name, TimeContol = new(max_time, add_time), ColorChoice = color };
        }
    }
}
