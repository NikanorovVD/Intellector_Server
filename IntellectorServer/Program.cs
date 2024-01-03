using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Text;

namespace IntellectorServer
{
    class Program
    {
        static TcpListener serverSocket;
        static Dictionary<uint, GameInfo> WaitingGames;
        static uint game_id = 1;
        static string password = "a3P1>8]Ы-/йЧяЭ975?:$qcDыФ9&e@1a<c{a/";
        static LogWriter logWriter;

        static void SendMesseage(byte mes, NetworkStream stream)
        {
            byte[] send_bytes = new byte[1] { mes };
            stream.Write(send_bytes, 0, 1);
        }

        static byte RecvMesseage(NetworkStream stream)
        {
            byte[] recv_bytes = new byte[1];
            stream.Read(recv_bytes, 0, 1);
            return recv_bytes[0];
        }

        static void Main(string[] args)
        {
            serverSocket = new TcpListener(System.Net.IPAddress.Any, 7001);
            WaitingGames = new Dictionary<uint, GameInfo>();

            logWriter = new LogWriter();
            logWriter.Start();

            while (true)
            {
                try
                {
                    logWriter.Write($"Ожидание подключения\nОжидает клиентов : {WaitingGames.Count}");
                    serverSocket.Start();
                    TcpClient clientSocket = serverSocket.AcceptTcpClient();
                    logWriter.Write($"{DateTime.Now} : Подключение установлено c {clientSocket.Client.RemoteEndPoint}");
                    ManageNewClient(clientSocket);
                }
                catch (Exception e)
                {
                    logWriter.Write(e.Message);
                }
            }
        }

        static void ManageNewClient(TcpClient client)
        {
            const byte no_such_game_ans = 99;
            const byte games_list_request = 100;
            const byte create_game_request = 0;

            if (!CheckPassword())
            {
                logWriter.Write("Неверный пароль");
                client.Close();
                logWriter.Write("Клиент отключен");
                return;
            }
            logWriter.Write("ПАРОЛЬ ВЕРНЫЙ");


            try
            {
                NetworkStream stream = client.GetStream();
                uint wanted_id = RecvMesseage(stream);
                if(wanted_id == games_list_request)
                {
                    logWriter.Write("Запрошен список игр");
                    SendGamesInfo(stream);
                    client.Close();
                }
                else if(wanted_id == create_game_request)
                {
                    logWriter.Write("Запрос создания игры");

                    string name = ReadName();
                    GameInfo game = new GameInfo(game_id, name, client);
                    WaitingGames.Add(game_id, game);

                    logWriter.Write($"Игра успешно создана: ID - {game.ID}, Name - {game.Name}");
                    game_id++; if (game_id == 100) game_id++;

                    game.WaitingManager = new Thread(() => CommunicateWithWaitingClient(client, game.ID));
                    game.WaitingManager.Start();
                }
                else
                {
                    if (WaitingGames.ContainsKey(wanted_id))
                    {
                        GameInfo wanted_game = WaitingGames[wanted_id];
                        WaitingGames.Remove(wanted_id);
                        wanted_game.WaitingManager.Join();
                        MakeGame(client, wanted_game.Client, wanted_id);
                    }
                    else
                    {
                        SendMesseage(no_such_game_ans, client.GetStream());
                        client.Close();
                    }
                }
            }
            catch(Exception e)
            {
                logWriter.Write(e.Message);
            }

            string ReadName()
            {
                int max_name_length = GameInfo.max_name_length;
                byte[] name_bytes = new byte[max_name_length];
                client.GetStream().Read(name_bytes, 0, max_name_length);
                return Encoding.Default.GetString(name_bytes).TrimEnd('\0');
            }

            bool CheckPassword()
            {
                try
                {
                    byte[] answer = new byte[100];
                    NetworkStream stream = client.GetStream();
                    stream.Read(answer, 0, 43);

                    string ans_string = Encoding.Default.GetString(answer[0..43]);
                    return ans_string == password;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        static void MakeGame(TcpClient client1, TcpClient client2, uint id)
        {
            try
            {
                NetworkStream stream1 = client1.GetStream();
                NetworkStream stream2 = client2.GetStream();

                const byte ans_white = 0;
                const byte ans_black = 1;
                SendMesseage(ans_white, stream1);
                SendMesseage(ans_black, stream2);
                logWriter.Write($"Отправлены номера команд");

                uint id_buff = id;
                Thread GameManager1 = new Thread(() => ManageGame(client1, client2, id_buff));
                Thread GameManager2 = new Thread(() => ManageGame(client2, client1, id_buff));

                GameManager1.Start();
                GameManager2.Start();
            }
            catch (Exception e)
            {
                WaitingGames.Remove(id);
                logWriter.Write(e.Message);
            }
        }

        static void SendGamesInfo(NetworkStream stream)
        {
            try
            {
                byte games_count = (byte)WaitingGames.Count;
                SendMesseage(games_count, stream);
                logWriter.Write($"Отправка {games_count} записей");

                foreach (GameInfo game in WaitingGames.Values)
                {
                    game.Send(stream);
                }
                logWriter.Write("Завершена успешно");
            }
            catch (Exception e)
            {
                logWriter.Write(e.Message);
            }
        }

        static void ManageGame(TcpClient in_client, TcpClient out_client, uint id)
        {
            const byte exit_code = 111;
            int hash = 0;
            int previous_hash = 0;
            int repeat = 0;
            byte[] move = new byte[5];

            try
            {
                NetworkStream in_stream = in_client.GetStream();
                NetworkStream out_stream = out_client.GetStream();

                while (move[0] != exit_code)
                {
                    in_stream.Read(move, 0, 5);   logWriter.Write($"Получен ход:   {MoveToString(move)}");
                    out_stream.Write(move, 0, 5); logWriter.Write($"Отправлен ход: {MoveToString(move)}");
                    if (CheckHash()) throw new Exception($"Потеряно соединение, игра {id} остановлена"); ;
                }
                logWriter.Write("Получено сообщение о выходе");
            }
            catch (Exception e)
            {
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

        static void CommunicateWithWaitingClient(TcpClient client, uint id)
        {
            const int checks_frequency_millisec = 2000;
            const byte continue_waiting_ans = 123;
            const byte expected_ans = 1;

            NetworkStream stream = client.GetStream();
            try
            {
                while (WaitingGames.ContainsKey(id))
                {
                    SendMesseage(continue_waiting_ans, stream);
                    byte client_ans = RecvMesseage(stream);
                    logWriter.Write($"ответ клиента: {client_ans}");
                    if (client_ans != expected_ans)
                    {
                        logWriter.Write($"Соединение потеряно ");
                        WaitingGames.Remove(id);
                        return;
                    }
                    Thread.Sleep(checks_frequency_millisec);
                }
            }
            catch (Exception e)
            {
                logWriter.Write($"Соединение потеряно: {e.Message}");
                WaitingGames.Remove(id);
                return;
            }
        }
    }
}
