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
    byte[] buffer = new byte[1000];
    static IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
    static IPEndPoint ServerEndpoint = new IPEndPoint(ipAddress, 32000);
    static EndPoint remoteEP = new IPEndPoint(ipAddress, 32000);

    //TODO: implement all necessary logic to create sockets and handle incoming messages
    // Do not put all the logic into one method. Create multiple methods to handle different tasks.
    public void start()

    {
        SendHelloMessage();
        
    }

    //TODO: [Send Hello message]
    public void SendHelloMessage()
{
    s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
    Message msg = new Message();
    msg.Type = MessageType.Hello;
    string json = JsonSerializer.Serialize(msg);
    byte[] data = Encoding.ASCII.GetBytes(json);
    s.SendTo(data, ServerEndpoint);
    Console.WriteLine("Hello message sent to the server");
    ReceiveMessage();
}
    
    public void ReceiveMessage()
    {
        while (true)
        {
            int recv = s.ReceiveFrom(buffer, ref remoteEP);
            string message = Encoding.ASCII.GetString(buffer, 0, recv);
            Console.WriteLine("Message received from the server: " + message);
            Message msg = JsonSerializer.Deserialize<Message>(message);
            switch (msg.Type)
            {
                case MessageType.Welcome:
                    SendRequestData();
                    break;
                case MessageType.Data:
                    HandleData();
                    break;
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
        }
            
    //TODO: [Receive Welcome]

    //TODO: [Send RequestData]
    public void SendRequestData()
    {
        while (true)
        {
            Message msg = new Message();
            msg.Type = MessageType.RequestData;
            string json = JsonSerializer.Serialize(msg);
            byte[] data = Encoding.ASCII.GetBytes(json);
            s.SendTo(data, ServerEndpoint);
            Console.WriteLine("RequestData message sent to the server");
            ReceiveMessage(); 
        }
        
    }

    //TODO: [Receive Data]
    public void HandleData()
    {
        Console.WriteLine("Data message received");
        Message msg = new Message();
        msg.Type = MessageType.End;
        msg.Content = "End of data";
        byte[] msgBytes = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(msg));
        s.SendTo(msgBytes, remoteEP);
        Console.WriteLine("End message sent to the server");
    }

    //TODO: [Send RequestData]

    //TODO: [Send End]

    //TODO: [Handle Errors]


}