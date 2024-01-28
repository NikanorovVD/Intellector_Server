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
        static LogWriter logWriter;

        
        static void Main(string[] args)
        {
            serverSocket = new TcpListener(System.Net.IPAddress.Any, 7002);
            WaitingGames = new Dictionary<uint, WaitingGame>();

            logWriter = new LogWriter();
            logWriter.Start();

            Game.logWriter = logWriter;
            while (true)
            {
                try
                {
                    logWriter.Write($"Ожидание подключения, Ожидает клиентов : {WaitingGames.Count}");
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
                uint wanted_id = RecvCode(stream);
                if(wanted_id == games_list_request)
                {
                    if (!CheckVersion())
                    {
                        client.Close();
                        return;
                    }

                    logWriter.Write("Запрошен список игр");
                    SendGamesInfo(stream);
                    client.Close();
                }
                else if(wanted_id == create_game_request)
                {
                    logWriter.Write("Запрос создания игры");
                   
                    GameInfo gameInfo = RecvGameInfo(stream);
                    WaitingGame game = new WaitingGame(game_id, gameInfo, client);
                    WaitingGames.Add(game_id, game);

                    logWriter.Write($"Игра успешно создана: {game.GameInfo}");
                    game_id++; if (game_id == 100) game_id++;

                    game.WaitingManager = new Thread(() => CommunicateWithWaitingClient(client, game.GameInfo.ID));
                    game.WaitingManager.Start();
                }
                else
                {
                    if (WaitingGames.ContainsKey(wanted_id))
                    {
                        WaitingGame wanted_game = WaitingGames[wanted_id];
                        WaitingGames.Remove(wanted_id);
                        wanted_game.WaitingManager.Join();
                        MakeGame(wanted_game, client);
                    }
                    else
                    {
                        SendCode(no_such_game_ans, client.GetStream());
                        client.Close();
                    }
                }
            }
            catch(Exception e)
            {
                logWriter.Write(e.Message);
            }

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
        }

        static void MakeGame(WaitingGame waiting_client, TcpClient new_client)
        {
            try
            {
                Game game = new Game(waiting_client, new_client);
                game.SendTeams();
                SendGameInfo(waiting_client.GameInfo, waiting_client.Client.GetStream());
                SendGameInfo(waiting_client.GameInfo, new_client.GetStream());
                game.Start();
            }
            catch (Exception e)
            {
                WaitingGames.Remove(waiting_client.GameInfo.ID);
                logWriter.Write(e.Message);
            }
        }

        static void SendGamesInfo(NetworkStream stream)
        {
            try
            {
                int games_count = WaitingGames.Count;
                SendInt(games_count, stream);
                logWriter.Write($"Отправка {games_count} записей");

                foreach (WaitingGame game in WaitingGames.Values)
                {
                    SendGameInfo(game.GameInfo, stream);
                } 
                logWriter.Write("Завершена успешно");
            }
            catch (Exception e)
            {
                logWriter.Write(e.Message);
            }
        }

        static void CommunicateWithWaitingClient(TcpClient client, uint id)
        {
            const int checks_frequency_millisec = 500;
            const byte continue_waiting_ans = 123;
            const byte expected_ans = 1;

            NetworkStream stream = client.GetStream();
            try
            {
                while (WaitingGames.ContainsKey(id))
                {
                    SendCode(continue_waiting_ans, stream);
                    byte client_ans = RecvCode(stream);
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
