using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IntellectorServer
{
    class GameInfo
    {
        public uint ID { get; set; }
        public string Name { get; set; }
        public TcpClient Client { get; set; }
        public Thread WaitingManager { get; set; }

        public static int max_name_length = 20;

        public GameInfo(uint iD, string name, TcpClient client)
        {
            ID = iD;
            Name = name;
            Client = client;
        }

        public void Send(NetworkStream stream)
        {
            stream.Write(BitConverter.GetBytes(ID), 0, 4);
            stream.Write(Encoding.Default.GetBytes(Name), 0, Encoding.Default.GetBytes(Name).Length);
        }
    }
}
