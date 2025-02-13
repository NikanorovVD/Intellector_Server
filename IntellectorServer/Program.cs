using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Extensions.Configuration;
using DataLayer;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using IntellectorServer.Models;
using IntellectorServer.Models.Errors;
using Microsoft.Extensions.DependencyInjection;
using IntellectorServer.Constants;
using IntellectorServer.Core;


namespace IntellectorServer
{
    class Program
    {
        private static TcpListener serverSocket;

        static void Main(string[] args)
        {
            var services = CreateServices();
            IConfiguration configuration = services.GetRequiredService<IConfiguration>();

            serverSocket = new TcpListener(System.Net.IPAddress.Any, int.Parse(configuration[Settings.Port]));

            while (true)
            {
                AsseptClient(configuration);
            }
        }

        private static ServiceProvider CreateServices()
        {
            string workingDirectory = Environment.CurrentDirectory;
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetParent(workingDirectory).Parent.Parent.FullName)
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Development.json", optional: true);

            IConfiguration configuration = builder.Build();

            var serviceProvider = new ServiceCollection()
                .AddDbContext<AppDbContext>(options => options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")))
                .AddSingleton(configuration)
                .BuildServiceProvider();

            return serviceProvider;
        }

        private static void AsseptClient(IConfiguration configuration)
        {
            try
            {
                serverSocket.Start();
                TcpClient clientSocket = serverSocket.AcceptTcpClient();

                Connection connection = new Connection(clientSocket);
                Message loginMessage = connection.ReceiveMessage();

                if (loginMessage.Method != Methods.Login)
                {
                    connection.Client.Close();
                    return;
                }

                LoginRequest loginRequest = JsonSerializer.Deserialize<LoginRequest>(loginMessage.Body);
                // аутентификация, уствновка User

                if (loginRequest.APIKey != configuration[Settings.APIKey])
                {
                    connection.Client.Close();
                }

                if (loginRequest.Version != configuration[Settings.Version])
                {
                    connection.SendMessage<VersionError>(Methods.Error, new()
                    {
                        ClientVersion = loginRequest.Version,
                        ServerVersion = configuration[Settings.Version]
                    });
                    connection.Client.Close();
                }

                TaskFactory taskFactory = new TaskFactory();
                Task clientManager = taskFactory.StartNew(
                    () => ManageClient(connection),
                    TaskCreationOptions.LongRunning
                    );
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        static void ManageClient(Connection connection)
        {
            TcpClient client = connection.Client;
            try
            {
                while (true)
                {
                    Message message = connection.ReceiveMessage();

                    switch (message.Method)
                    {
                        //Methods
                        default: throw new Exception($"Unexpected method: {message.Method}");
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
