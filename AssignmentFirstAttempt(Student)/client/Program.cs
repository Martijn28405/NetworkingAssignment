using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using MessageNS;

// SendTo();
class Program
{
    static void Main(string[] args)
    {
        ClientUDP cUDP = new ClientUDP();
        cUDP.start();
    }
}

class ClientUDP
{
    //TODO: create all needed objects for your sockets
    byte[] buffer = new byte[1000];
    Socket sock;
    static IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
    static IPEndPoint ServerEndpoint = new IPEndPoint(ipAddress,32000);
    static IPEndPoint sender = new IPEndPoint(ipAddress, 32000);
    static EndPoint remoteEP = (EndPoint)sender;

    //TODO: implement all necessary logic to create sockets and handle incoming messages
    // Do not put all the logic into one method. Create multiple methods to handle different tasks.
    public void start()

    {
        SendHelloMessage();
        
    }

    //TODO: [Send Hello message]
    public void SendHelloMessage()
{
    sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
    Message msg = new Message();
    msg.Type = MessageType.Hello;
    string json = JsonSerializer.Serialize(msg);
    byte[] data = Encoding.ASCII.GetBytes(json);
    sock.SendTo(data, ServerEndpoint);
    Console.WriteLine("Hello message sent to the server");
    ReceiveMessage();
}
    
    public void ReceiveMessage()
    {
        try
        {
            int recv = sock.ReceiveFrom(buffer, ref remoteEP);
            string message = Encoding.ASCII.GetString(buffer, 0, recv);
            Console.WriteLine("Message received from the server: " + message);
            Message msg = JsonSerializer.Deserialize<Message>(message);
            HandleMessage(msg);
        }
        catch (SocketException ex)
        {
            if (ex.SocketErrorCode == SocketError.ConnectionReset)
            {
                Console.WriteLine("Connection was forcibly closed by the remote host. Attempting to reconnect...");
                // Attempt to reconnect or handle the error appropriately
                
            }
            else
            {
                throw;
            }
        }
    }

    public void HandleMessage(Message msg)
    {
        switch (msg.Type)
        {
            case MessageType.Welcome:
                HandleWelcome();
                break;
            // case MessageType.Data:
            //     HandleData();
            //     break;
            // case MessageType.Ack:
            //     HandleAck();
            //     break;
            // case MessageType.End:
            //     HandleEnd();
            //     break;
            // case MessageType.Error:
            //     HandleError();
            //     break;
        }
    }
    //TODO: [Receive Welcome]
    public void HandleWelcome()
    {
        Console.WriteLine("Welcome message received");
        Message msg = new Message();
        msg.Type = MessageType.RequestData;
        msg.Content = "Requesting data";
        byte[] msgBytes = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(msg));
        sock.SendTo(msgBytes, remoteEP);
        SendRequestData();
    }

    //TODO: [Send RequestData]
    public void SendRequestData()
    {
        Message msg = new Message();
        msg.Type = MessageType.RequestData;
        string json = JsonSerializer.Serialize(msg);
        byte[] data = Encoding.ASCII.GetBytes(json);
        sock.SendTo(data, ServerEndpoint);
        Console.WriteLine("RequestData message sent to the server");
        ReceiveMessage();
    }

    //TODO: [Receive Data]

    //TODO: [Send RequestData]

    //TODO: [Send End]

    //TODO: [Handle Errors]


}