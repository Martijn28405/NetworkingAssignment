using System;
using System.Data.SqlTypes;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using MessageNS;


// Do not modify this class
class Program
{
    static void Main(string[] args)
    {
        ServerUDP sUDP = new ServerUDP();
        sUDP.start();
    }
}

class ServerUDP
{
    //TODO: create all needed objects for your sockets 
    private byte[] buffer = new byte[1000];
    private Socket sock;
    private static IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
    private static IPEndPoint serverIpEndPoint = new IPEndPoint(ipAddress, 32000);
    private EndPoint remoteEP = new IPEndPoint(ipAddress, 32000);

    //TODO: implement all necessary logic to create sockets and handle incoming messages
    // Do not put all the logic into one method. Create multiple methods to handle different tasks.
    public void start()
    {
        ReceiveMessage();
    }

    //TODO: keep receiving messages from clients
    public void ReceiveMessage()
    {
        Console.WriteLine("server is listening on port 32000");
        sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        sock.Bind(serverIpEndPoint);

        while (true)
        {
            Console.WriteLine("server is waiting for a message.....");
            int recv = sock.ReceiveFrom(buffer, ref remoteEP);
            string message = Encoding.ASCII.GetString(buffer, 0, recv);
            Message msg = JsonSerializer.Deserialize<Message>(message);
            switch (msg.Type)
            {
                case MessageType.Hello:
                    HandleHello();
                    break;
                case MessageType.RequestData:
                    HandleRequestData();
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

    // you can call a dedicated method to handle each received type of messages
    //TODO: [Receive Hello]
    //TODO: [Send Welcome]
    public void HandleHello()
    {
        Console.WriteLine("Hello message received");
        Message msg = new Message();
        msg.Type = MessageType.Welcome;
        msg.Content = "Welcome to the server";
        byte[] msgBytes = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(msg));
        sock.SendTo(msgBytes, remoteEP);
    }

    

    //TODO: [Receive RequestData]
    public void HandleRequestData()
    {
        while (true)
        {

            Console.WriteLine("RequestData message received");
            Message msg = new Message();
            msg.Type = MessageType.Data;
            msg.Content = "Sending data";
            byte[] msgBytes = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(msg));
            sock.SendTo(msgBytes, remoteEP);
        }
    }

    //TODO: [Send Data]

    //TODO: [Implement your slow-start algorithm considering the threshold] 

    //TODO: [End sending data to client]

    //TODO: [Handle Errors]

    //TODO: create all needed methods to handle incoming messages


}