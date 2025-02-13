using IntellectorServer.Models;
using System;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;
using static IntellectorServer.Core.Networking;

namespace IntellectorServer.Core
{
    public class Connection
    {
        public TcpClient Client { get; set; }
        public NetworkStream Stream { get => Client.GetStream(); }
        public User User { get; set; }

        public Connection(TcpClient tcpClient)
        {
            Client = tcpClient;
        }

        public void SendMessage(Message message)
        {
            string json = JsonSerializer.Serialize(message);
            SendString(json, Stream);
        }

        public async Task SendMessageAsync(Message message)
        {
            string json = JsonSerializer.Serialize(message);
            await SendStringAsync(json, Stream);
        }

        public void SendMessage<T>(string method, T content)
        {
            Message message = new()
            {
                Method = method,
                Body = JsonSerializer.Serialize(content)
            };
            SendMessage(message);
        }

        public async Task SendMessageAsync<T>(string method, T content)
        {
            Message message = new()
            {
                Method = method,
                Body = JsonSerializer.Serialize(content)
            };
            await SendMessageAsync(message);
        }

        public Message ReceiveMessage()
        {
            string content = RecvString(Stream);
            Console.WriteLine(content);
            return JsonSerializer.Deserialize<Message>(content);
        }

        public async Task<Message> ReceiveMessageAsync()
        {
            string content = await RecvStringAsync(Stream);
            return JsonSerializer.Deserialize<Message>(content);
        }

        public (string method, T content) ReceiveMessage<T>()
        {
            string content = RecvString(Stream);
            Message message = JsonSerializer.Deserialize<Message>(content);
            return (message.Method, JsonSerializer.Deserialize<T>(message.Body));
        }

        public async Task<(string method, T content)> ReceiveMessageAsync<T>()
        {
            string content = await RecvStringAsync(Stream);
            Message message = JsonSerializer.Deserialize<Message>(content);
            return (message.Method, JsonSerializer.Deserialize<T>(message.Body));
        }
    }
}
