using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

public class Client
{
    public Socket Socket { get; set; }
    public string Nick { get; set; }

    public Client(Socket socket)
    {
        Socket = socket;
        Nick = $"user:{socket.Handle.ToInt32()}";
    }
}

public class ChatServer
{
    private const int MaxClients = 1000;
    private const int ServerPort = 7711;
    private TcpListener listener;
    private readonly Client[] clients = new Client[MaxClients];
    private int numClients;
    private int maxClient;

    public ChatServer()
    {
        listener = new TcpListener(IPAddress.Any, ServerPort);
        listener.Start();
        Console.WriteLine($"Server is listening on port {ServerPort}");
    }

    public void Start()
    {
        while (true)
        {
            var clientSocket = listener.AcceptSocket();
            Console.WriteLine($"Connected client: {clientSocket.RemoteEndPoint}");

            var client = new Client(clientSocket);
            clients[numClients] = client;
            numClients++;
            maxClient = (int)numClients;
            Task.Factory.StartNew(() => HandleClient(client));
        }
    }

    public async Task HandleClient(Client client)
    {
        using (NetworkStream stream = new NetworkStream(client.Socket))
        {
            byte[] buffer = new byte[1024];
            int bytesRead;

            while (true)
            {
                try
                {
                    bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                }
                catch (Exception)
                {
                    break;
                }

                if (bytesRead == 0)
                    break;
                string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine($"Received from {client.Socket.RemoteEndPoint}: {message}");

                foreach (var otherClient in clients)
                {
                    if (otherClient != null && otherClient != client)
                    {
                        try
                        {
                            await SendToClientAsync(otherClient, message);
                        }
                        catch (Exception)
                        {
                            // Handle exceptions when sending to a client
                        }
                    }
                }
            }

            Console.WriteLine($"Disconnected client: {client.Socket.RemoteEndPoint}");
            var idx = Array.FindIndex(clients, x => x.Socket.Handle.ToInt32() == client.Socket.Handle.ToInt32());
            if (idx >= 0)
            {
                clients[idx] = null;
            }
            
            numClients--;

            maxClient = numClients;
        }
    }

    public async Task SendToClientAsync(Client client, string message)
    {
        byte[] data = Encoding.UTF8.GetBytes(message);
        await client.Socket.SendAsync(data, SocketFlags.None);
    }
}

class SmallChat
{
    static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        var chatServer = new ChatServer();
        chatServer.Start();
    }
}
