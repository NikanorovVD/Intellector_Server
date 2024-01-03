using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Text;

namespace Server
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
    class Program
    {
        static TcpListener serverSocket;
        static Dictionary<uint, GameInfo> WaitingGames;
        static uint game_id = 1;
        static string password = "a3P1>8]Ы-/йЧяЭ975?:$qcDыФ9&e@1a<c{a/";

        static Queue<string> LogQueue;
        static string LogFilePath = "log.txt";
        static void WriteLog(string messeage)
        {
            Console.WriteLine(messeage);
            using (StreamWriter writer = new StreamWriter(LogFilePath, true))
            {
                writer.WriteLine(messeage);
            }
        }

        static void Main(string[] args)
        {
            serverSocket = new TcpListener(System.Net.IPAddress.Any, 7001);
            LogQueue = new Queue<string>();
            WaitingGames = new Dictionary<uint, GameInfo>(); 

            Thread LogWriter = new Thread(WriteLogs);
            LogWriter.Start();

            while (true)
            {
                try
                {
                    LogQueue.Enqueue("Ожидание подключения");
                    LogQueue.Enqueue($"Ожидает клиентов : {WaitingGames.Count}");
                    serverSocket.Start();
                    TcpClient clientSocket = serverSocket.AcceptTcpClient();
                    LogQueue.Enqueue($"{DateTime.Now} : Подключение установлено c {clientSocket.Client.RemoteEndPoint}");

                    ManageNewClient(clientSocket);
                }
                catch (Exception e)
                {
                    LogQueue.Enqueue(e.Message);
                }
            }
        }

        static void ManageNewClient(TcpClient client)
        {
            if (!CheckPassword())
            {
                LogQueue.Enqueue("Неверный пароль");
                client.Close();
                LogQueue.Enqueue("Клиент отключен");
                return;
            }
            LogQueue.Enqueue("Пароль верный");
            byte[] get_bytes = new byte[1];
            try
            {
                NetworkStream stream = client.GetStream();
                stream.Read(get_bytes, 0, 1);
                uint wanted_id = get_bytes[0];
                if(wanted_id == 100)
                {
                    LogQueue.Enqueue("Запрошен список игр");
                    SendGamesInfo(stream);
                    client.Close();
                }
                else if(wanted_id == 0)
                {
                    LogQueue.Enqueue("Запрос создания игры");
                    GameInfo game = CreateNewGame();
                    WaitingGames.Add(game_id, game);
                    LogQueue.Enqueue($"Игра успешно создана: ID - {game.ID}, Name - {game.Name}");
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
                        client.GetStream().Write(new byte[] { 99 }, 0, 1);
                        client.Close();
                    }
                }
            }
            catch(Exception e)
            {
                LogQueue.Enqueue(e.Message);
            }

            GameInfo CreateNewGame()
            {
                byte[] s = new byte[20];
                client.GetStream().Read(s, 0, 20);
                string name = Encoding.Default.GetString(s).TrimEnd('\0'); 
                return new GameInfo(game_id, name, client);
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

                byte[] ans1 = new byte[1] { 0 };
                byte[] ans2 = new byte[1] { 1 };
                stream1.Write(ans1, 0, 1);
                stream2.Write(ans2, 0, 1);
                LogQueue.Enqueue($"Сервером отправлены сообщения: {ans1[0]} , {ans2[0]}");

                uint id_buff = id;
                Thread GameManager1 = new Thread(() => ManageGame(client1, client2, id_buff));
                Thread GameManager2 = new Thread(() => ManageGame(client2, client1, id_buff));

                GameManager1.Start();
                GameManager2.Start();
            }
            catch (Exception e)
            {
                WaitingGames.Remove(id);
                LogQueue.Enqueue(e.Message);
            }
        }

        static void SendGamesInfo(NetworkStream stream)
        {
            byte[] GameCount = new byte[1] { (byte)WaitingGames.Count };
            try
            {
                stream.Write(GameCount, 0, 1);
                LogQueue.Enqueue($"Отправка {GameCount[0]} записей");
                foreach (GameInfo game in WaitingGames.Values)
                {
                    game.Send(stream);
                }
                LogQueue.Enqueue("Завершена успешно");
            }
            catch (Exception e)
            {
                LogQueue.Enqueue(e.Message);
            }
        }

        static void ManageGame(TcpClient in_client, TcpClient out_client, uint id)
        {
            int hash = 0;
            int previous_hash = 0;
            int repeat = 0;
            byte[] move = new byte[5];

            try
            {
                NetworkStream in_stream = in_client.GetStream();
                NetworkStream out_stream = out_client.GetStream();

                while (move[0] != 111)
                {
                    in_stream.Read(move, 0, 5); LogQueue.Enqueue($"Получен ход: {MoveToString(move)}");
                    out_stream.Write(move, 0, 5); LogQueue.Enqueue($"Отправлен ход: {MoveToString(move)}");
                    if (CheckHash()) throw new Exception($"Потеряно соединение, игра {id} остановлена"); ;
                }
                LogQueue.Enqueue("Получено сообщение о выходе");
            }
            catch (Exception e)
            {
                LogQueue.Enqueue(e.Message);
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
            byte[] ContinueWaiting = new byte[1] { 123 };
            byte[] ClientAnswer = new byte[1];
            byte expected_ans = 1;
            while (WaitingGames.ContainsKey(id))
            {
                try
                {
                    ClientAnswer[0] = 0;

                    NetworkStream stream = client.GetStream();
                    stream.Write(ContinueWaiting, 0, 1);
                    stream.Read(ClientAnswer, 0, 1);
                    LogQueue.Enqueue($"ответ клиента: {ClientAnswer[0]}");
                    if (ClientAnswer[0] != expected_ans)
                    {
                        LogQueue.Enqueue($"Соединение потеряно ");
                        WaitingGames.Remove(id);
                        return;
                    }
                }
                catch (Exception e)
                {
                    LogQueue.Enqueue($"Соединение потеряно ");
                    LogQueue.Enqueue(e.Message);
                    WaitingGames.Remove(id);
                    return;
                }
                Thread.Sleep(2000);
            }
        }
        static void WriteLogs()
        {
            while (true)
            {
                if (LogQueue.Count != 0)
                {
                    WriteLog(LogQueue.Dequeue());
                }
            }
        }
    }
}
