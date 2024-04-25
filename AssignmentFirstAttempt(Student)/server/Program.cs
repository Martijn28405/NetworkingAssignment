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
    private byte[] buffer = new byte[66000];
    private Socket sock;
    private static IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
    private static IPEndPoint serverIpEndPoint = new IPEndPoint(ipAddress, 32000);
    private EndPoint remoteEP = new IPEndPoint(ipAddress, 32000);

    private static int threshold = 20;
    private int congestionWindow = threshold;
    private int duplicateAckCount = 0;


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
                    SendData();
                    break;
                case MessageType.Ack:
                    HandleAck();
                    break;
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

    

    
    public void SendData()
    {
        ImplementSlowStart();
        string filePath = "hamlet.txt";
        string[] lines = File.ReadAllLines(filePath);

        int chunkSize = 100; // Define the size of each message chunk
        int subChunkSize = 10; // Define the size of each sub-chunk

        for (int i = 0; i < lines.Length; i += chunkSize)
        {
            string[] chunkLines = lines.Skip(i).Take(chunkSize).ToArray();

            for (int j = 0; j < chunkLines.Length; j += subChunkSize)
            {
                string[] subChunkLines = chunkLines.Skip(j).Take(subChunkSize).ToArray();
                string subChunkContent = string.Join("\n", subChunkLines);

                Message msg = new Message
                {
                    Type = MessageType.Data,
                    Content = subChunkContent
                };

                byte[] msgBytes = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(msg));
                sock.SendTo(msgBytes, remoteEP);
            }
        }

        SendEnd();
    }

    public void HandleAck()
    {
        Console.WriteLine("Ack message received");
        duplicateAckCount = 0;
        congestionWindow++;
        if (congestionWindow > threshold)
        {
            congestionWindow = threshold;
            SendData();
        }
        
    }

    //TODO: [Send Data]

    //TODO: [Implement your slow-start algorithm considering the threshold]
    public void ImplementSlowStart()
    {
        Console.WriteLine("Implementing slow-start algorithm");

        // Perform slow-start algorithm logic here
        // You can use the congestionWindow and threshold variables

        // Example implementation:
        if (congestionWindow < threshold)
        {
            congestionWindow *= 2;
        }
    }
    


    //TODO: [End sending data to client]
    public void SendEnd()
    {
        Console.WriteLine("Send end message");
        // Perform any necessary cleanup or finalization here
        // For example, close the socket or release any resources
        Message msg = new Message();
        msg.Type = MessageType.End;
        byte[] msgBytes = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(msg));
        sock.SendTo(msgBytes, remoteEP);
        
    }

    //TODO: [Send Error]
    public void SendError()
    {
        Console.WriteLine("Send error message");
        Message msg = new Message();
        msg.Type = MessageType.Error;
        byte[] msgBytes = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(msg));
        sock.SendTo(msgBytes, remoteEP);
        
    }


    //TODO: [Handle Errors]

    //TODO: create all needed methods to handle incoming messages


}