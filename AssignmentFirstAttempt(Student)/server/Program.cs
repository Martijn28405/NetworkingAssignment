using System;
using System.Data.SqlTypes;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using MessageNS;
using Microsoft.VisualBasic;


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

    private static int threshold = 0;
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
            Console.WriteLine("Received message: " + message);
            Message msg = JsonSerializer.Deserialize<Message>(message);
            switch (msg.Type)
            {
                case MessageType.Hello:
                    HandleHello(msg);
                    break;
                case MessageType.RequestData:
                    SendData();
                    break;
                // case MessageType.Ack:
                //     HandleAck();
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
    public void HandleHello(Message msg)
    {
        Console.WriteLine("Hello message received");

        if (msg.Content != null)
        {
            threshold = int.Parse(msg.Content);
            SendWelcome();
        }
        // HandleError();

        

        
    }

    public void SendWelcome()
    {
        Message welcome_message = new Message();
        welcome_message.Type = MessageType.Welcome;
        byte[] msgBytes = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(welcome_message));
        sock.SendTo(msgBytes, remoteEP);
        Console.WriteLine("Send welcome message");
    }

    

    
    public void SendData()
    {
        string filePath = "hamlet.txt";
        string[] lines = File.ReadAllLines(filePath);
         // Assign a unique message ID for each message

        int chunkSize = 100; // Define the size of each message chunk
        int subChunkSize = 10; // Define the size of each sub-chunk

        int totalChunks = (int)Math.Ceiling((double)lines.Length / chunkSize);
        int subChunkIndex = 0;

        for (int chunkIndex = 0; chunkIndex < totalChunks; chunkIndex++)
        {
            int startIndex = chunkIndex * chunkSize;
            int endIndex = Math.Min(startIndex + chunkSize, lines.Length);

            string[] chunkLines = lines[startIndex..endIndex];

            int totalSubChunks = (int)Math.Ceiling((double)chunkLines.Length / subChunkSize);

            for (subChunkIndex = 0; subChunkIndex < totalSubChunks; subChunkIndex++)
            {
                int subChunkStartIndex = subChunkIndex * subChunkSize;
                int subChunkEndIndex = Math.Min(subChunkStartIndex + subChunkSize, chunkLines.Length);

                string[] subChunkLines = chunkLines[subChunkStartIndex..subChunkEndIndex];
                string subChunkContent = string.Join("\n", subChunkLines);

                Message msg = new Message
                {
                    Type = MessageType.Data,
                    Content = chunkIndex + "" + subChunkContent
                };

                byte[] msgBytes = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(msg));
                sock.SendTo(msgBytes, remoteEP);
            }

            // if()
            // {
            //     SendEnd();
            // }
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
    


    //TODO: [End sending data to client]

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