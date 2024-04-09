using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Text;
using static IntellectorServer.Networking;

namespace IntellectorServer
{
    class Program
    {
        static int server_version = 14;
        static TcpListener serverSocket;
        static Dictionary<uint, WaitingGame> WaitingGames;
        static uint game_id = 1;
        static string password = "a3P1>8]Ы-/йЧяЭ975?:$qcDыФ9&e@1a<c{a/";

        
        static void Main(string[] args)
        {
            serverSocket = new TcpListener(System.Net.IPAddress.Any, 7002);
            WaitingGames = new Dictionary<uint, WaitingGame>();

            LogWriter.WriteLine("Server Start");
            while (true)
            {
                try
                {
                    serverSocket.Start();
                    TcpClient clientSocket = serverSocket.AcceptTcpClient();
                    LogWriter.WriteLine($"{DateTime.Now} : Подключение установлено c {clientSocket.Client.RemoteEndPoint}");

                    Thread clientManager = new Thread(() => ManageNewClient(clientSocket));
                    clientManager.Start();
                }
                catch (Exception e)
                {
                    LogWriter.WriteLine(e.Message);
                }
            }
        }

        static void ManageNewClient(TcpClient client)
        {
            const byte games_list_request = 100;
            const byte join_game_request = 30;
            const byte create_game_request = 40;

            try
            {
                ValidateClient(client);
                while (true)
                {
                    byte request_code = RecvCode(client.GetStream());
                    switch (request_code)
                    {
                        case games_list_request: GamesListRequest(client); break;
                        case create_game_request: CreateGameRequest(client); return;
                        case join_game_request: JoinGameRequest(client); return;
                        case 0: return;
                    }
                }
            }
            catch(Exception e)
            {
                LogWriter.WriteLine(e.Message);
            }
        }

        static void ValidateClient(TcpClient client)
        {

            bool CheckPassword()
            {
                try
                {
                    string ans_string = RecvString(client.GetStream());
                    return ans_string == password;
                }
                catch (Exception)
                {
                    return false;
                }
            }

            bool CheckVersion()
            {
                int client_version = RecvInt(client.GetStream());
                SendInt(server_version, client.GetStream());
                return client_version == server_version;
            }

            if (!CheckPassword())
            {
                LogWriter.WriteLine("Неверный пароль, Клиент отключен");
                client.Close();
                return;
            }
            if (!CheckVersion())
            {
                LogWriter.WriteLine("Неподходящая версия клиента");
                client.Close();
                return;
            }
            LogWriter.WriteLine("ПАРОЛЬ ВЕРНЫЙ");
        }
        static void GamesListRequest(TcpClient client)
        {
            LogWriter.WriteLine("Запрос списка игр");
            SendGamesInfo(client.GetStream());
        }
        static void CreateGameRequest(TcpClient client)
        {
            LogWriter.WriteLine("Запрос создания игры");

            GameInfo gameInfo = RecvGameInfo(client.GetStream());
            WaitingGame game = new WaitingGame(game_id, gameInfo, client);
            WaitingGames.Add(game_id, game);

            LogWriter.WriteLine($"Игра успешно создана: {game.GameInfo}");
            game_id++; 

            CommunicateWithWaitingClient(client, game.GameInfo.ID);
        }
        static void JoinGameRequest(TcpClient client)
        {
            const byte no_such_game_ans = 99;

            LogWriter.WriteLine("Запрос присоединения к игре");
            byte wanted_id = RecvCode(client.GetStream());

            if (WaitingGames.ContainsKey(wanted_id))
            {
                WaitingGame wanted_game = WaitingGames[wanted_id];
                WaitingGames.Remove(wanted_id);

                SendGameInfo(wanted_game.GameInfo, client.GetStream());

                LogWriter.WriteLine($"Старт игры {wanted_id}");
                MakeGame(wanted_game, client);
            }
            else
            {
                SendCode(no_such_game_ans, client.GetStream());
            }
        }

        static void MakeGame(WaitingGame waiting_client, TcpClient new_client)
        {
            try
            {
                Game game = new Game(waiting_client, new_client);
                game.SendTeams();
                game.Start();
            }
            catch (Exception e)
            {
                WaitingGames.Remove(waiting_client.GameInfo.ID);
                LogWriter.WriteLine(e.Message);
            }
        }

        static void SendGamesInfo(NetworkStream stream)
        {
            try
            {
                SendInt(WaitingGames.Count, stream);
                foreach (WaitingGame game in WaitingGames.Values)
                {
                    SendGameInfo(game.GameInfo, stream);
                } 
            }
            catch (Exception e)
            {
                LogWriter.WriteLine(e.Message);
            }
        }

        static void CommunicateWithWaitingClient(TcpClient client, uint id)
        {
            const int checks_frequency_millisec = 500;
            const byte continue_waiting_ans = 123;
            const byte expected_ans = 1;

            try
            {
                NetworkStream stream = client.GetStream();
                while (WaitingGames.ContainsKey(id))
                {
                    SendCode(continue_waiting_ans, stream);
                    byte client_ans = RecvCode(stream);
                    LogWriter.WriteLine($"ответ клиента: {client_ans}");
                    if (client_ans != expected_ans)
                    {
                        LogWriter.WriteLine($"отмена ожидания");
                        client.Close();
                        WaitingGames.Remove(id);
                        return;
                    }
                    Thread.Sleep(checks_frequency_millisec);
                }
            }
            catch (Exception e)
            {
                LogWriter.WriteLine(e.Message);
                WaitingGames.Remove(id);
                return;
            }
        }
    }
}
