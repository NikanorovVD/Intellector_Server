using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IntellectorServer
{
    public enum ColorChoice
    {
        white = 0,
        black = 1,
        random = 2
    }

    public class WaitingGame
    {
        public GameInfo GameInfo { get; private set; }
        public TcpClient Client { get; set; }
        public Thread WaitingManager { get; set; }

        public static int max_name_length = 20;

        public WaitingGame(uint id, GameInfo gameInfo, TcpClient client)
        {
            GameInfo = gameInfo;
            gameInfo.ID = id;
            Client = client;
        }
    }
}
