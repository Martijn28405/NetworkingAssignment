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
    Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
    byte[] buffer = new byte[66000];
    static IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
    static IPEndPoint ServerEndpoint = new IPEndPoint(ipAddress, 32000);
    static EndPoint remoteEP = new IPEndPoint(ipAddress, 32000);

    public int threshold = 20;
    //TODO: implement all necessary logic to create sockets and handle incoming messages
    // Do not put all the logic into one method. Create multiple methods to handle different tasks.
    public void start()

    {
        SendHelloMessage();
        ReceiveMessage();
    }

    //TODO: [Send Hello message]
    public void SendHelloMessage()
    {
        s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        Message msg = new Message();
        msg.Type = MessageType.Hello;
        msg.Content = threshold.ToString(); // Default threshold value
        string json = JsonSerializer.Serialize(msg);
        byte[] data = Encoding.ASCII.GetBytes(json);
        s.SendTo(data, ServerEndpoint);
        Console.WriteLine("Hello message sent to the server");
    }
    
    public void ReceiveMessage()
    {
        while (true)
        {
            int recv = s.ReceiveFrom(buffer, ref remoteEP);
            string message = Encoding.ASCII.GetString(buffer, 0, recv);
            Message msg = JsonSerializer.Deserialize<Message>(message);
            switch (msg.Type)
            {
                case MessageType.Welcome:
                    Console.WriteLine("Welcome message received");
                    SendRequestData();
                    break;
                case MessageType.Data:
                    HandleData(msg);
                    break;
                case MessageType.End:
                    HandleEnd();
                    break;
                // case MessageType.Error:
                //     HandleError();
                //     break;
            }
            
        }
        }
            
    //TODO: [Receive Welcome]

    //TODO: [Send RequestData]
    public void SendRequestData()
    {
        Message msg = new Message();
        msg.Type = MessageType.RequestData;
        msg.Content = "hamlet.txt";
        string json = JsonSerializer.Serialize(msg);
        byte[] data = Encoding.ASCII.GetBytes(json);
        s.SendTo(data, ServerEndpoint);
        Console.WriteLine("RequestData message sent to the server"); 
    }

    //TODO: [Receive Data]
    public void HandleData(Message msg)
    {
        Console.WriteLine("Data message received");
        string data = msg.Content;
        File.AppendAllText("output.txt", data);
        Console.WriteLine("Data saved to output.txt");
    }

    public void SendAck(int id)
    {
        Message msg = new Message();
        msg.Type = MessageType.Ack;
        msg.Content = id.ToString();
        string json = JsonSerializer.Serialize(msg);
        byte[] data = Encoding.ASCII.GetBytes(json);
        s.SendTo(data, ServerEndpoint);
        Console.WriteLine("Ack message sent to the server");
    }

    //TODO: [Send End]
    public void HandleEnd()
    {
        Console.WriteLine("End message received");
        // Perform any necessary cleanup or finalization here
        // For example, close the socket or release any resources
        Environment.Exit(0);

    }

    //TODO: [Send Error]
    public void HandleError()
    {
        Console.WriteLine("Error message received");
        
        // Perform any necessary error handling here
    
        
    }


    //TODO: [Handle Errors]


}