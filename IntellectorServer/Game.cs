using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static IntellectorServer.Networking;


namespace IntellectorServer
{
    class Game
    {
        public static LogWriter logWriter;
        public uint ID;
        public TcpClient WhitePlayer { get; private set; }
        public TcpClient BlackPlayer { get; private set; }

        private TimeController time_controller;

        private Thread WhiteGameManager;
        private Thread BlackGameManager;

        public Game(WaitingGame waiting_game, TcpClient client)
        {
            ID = waiting_game.GameInfo.ID;
            SetPlayers();
            if(waiting_game.GameInfo.TimeContol.MaxMinutes == 0)
            {
                time_controller = null;
            }
            else
            {
                time_controller = new TimeController(waiting_game.GameInfo.TimeContol);
                time_controller.TimeOutEvent += SendTimeOut;
            }
            WhiteGameManager = new Thread(() => ManageGame(WhitePlayer, BlackPlayer, false));
            BlackGameManager = new Thread(() => ManageGame(BlackPlayer, WhitePlayer, true));


            void SetPlayers()
            {
                switch (waiting_game.GameInfo.ColorChoice)
                {
                    case ColorChoice.white:
                        WhitePlayer = waiting_game.Client;
                        BlackPlayer = client;
                        break;
                    case ColorChoice.black:
                        BlackPlayer = waiting_game.Client;
                        WhitePlayer = client;
                        break;
                    case ColorChoice.random:
                        if (new Random().Next(0, 2) == 0) 
                            goto case ColorChoice.white;
                        else goto case ColorChoice.black;
                }
            }
        }
        public void Start()
        {
            WhiteGameManager.Start();
            BlackGameManager.Start();
            time_controller?.Start();
        }

        public void SendTeams()
        {
            SendCode(0, WhitePlayer.GetStream());
            SendCode(1, BlackPlayer.GetStream());
        }

        private void ManageGame(TcpClient in_client, TcpClient out_client, bool team)
        {
            const byte move_code = 10;
            const byte exit_code = 111;
            const byte rematch_code = 222;

            byte input_code = 0;
            int hash = 0;
            int previous_hash = 0;
            int repeat = 0;
            byte[] move = new byte[5];

            try
            {
                NetworkStream in_stream = in_client.GetStream();
                NetworkStream out_stream = out_client.GetStream();

                while (input_code != exit_code)
                {
                    input_code = RecvCode(in_stream);
                    switch (input_code)
                    {
                        case move_code:
                            move = RecvMove(in_stream); logWriter.Write($"Получен ход:   {MoveToString(move)}");
                            if (team) time_controller?.BlackMakeMove();
                            else time_controller?.WhiteMakeMove();

                            SendMove(move, out_stream); logWriter.Write($"Отправлен ход: {MoveToString(move)}");
                            if (time_controller != null)
                            {
                                int time = (team) ? time_controller.BlackTime : time_controller.WhiteTime;
                                SendTime(time, out_stream);
                                SendTime(time, in_stream);
                            }
                            if (CheckHash()) throw new Exception($"Потеряно соединение, игра {ID} остановлена");
                            break;
                        case rematch_code:
                            SendCode(rematch_code, out_stream);
                            team = !team;
                            time_controller?.Start();
                            break;
                        case exit_code:
                            SendCode(exit_code, out_stream);
                            time_controller?.Stop();
                            break;
                        default: throw new Exception($"Получен неизвестный код : {input_code}");
                    }
                    
                }
                logWriter.Write("Получено сообщение о выходе");
            }
            catch (Exception e)
            {
                time_controller?.Stop();
                logWriter.Write(e.Message);
                return;
            }

            string MoveToString(byte[] move)
            {
                string res = String.Empty;
                foreach (byte b in move)
                {
                    res += b.ToString() + ' ';
                }
                return res;
            }

            bool CheckHash()
            {
                hash = move[0] + move[1] + move[2] + move[3] + move[4];
                if (hash == previous_hash) repeat++;
                else repeat = 0;
                previous_hash = hash;
                return (repeat >= 10);
            }
        }

        private void SendTimeOut(bool team)
        {
            const byte white_time_out_code = 30;
            const byte black_time_out_code = 31;
            if (team)
            {
                SendCode(black_time_out_code, BlackPlayer.GetStream());
                SendCode(black_time_out_code, WhitePlayer.GetStream());
            }
            else
            {
                SendCode(white_time_out_code, WhitePlayer.GetStream());
                SendCode(white_time_out_code, BlackPlayer.GetStream());
            }
        }
    }
}
