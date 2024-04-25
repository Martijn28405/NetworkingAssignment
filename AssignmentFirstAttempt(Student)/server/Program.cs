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
    private static IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
    private static IPEndPoint serverIpEndPoint = new IPEndPoint(ipAddress, 32000);
    private EndPoint remoteEP = new IPEndPoint(ipAddress, 32000);

    Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);


    static int threshold = 0;


    //TODO: implement all necessary logic to create sockets and handle incoming messages
    // Do not put all the logic into one method. Create multiple methods to handle different tasks.
    public void start()
    {
        sock.Bind(serverIpEndPoint);
        ReceiveMessage();
    }

    //TODO: keep receiving messages from clients
    public void ReceiveMessage()
    {
        Console.WriteLine("server is listening on port 32000");

        while (true)
        {
            Console.WriteLine("server is waiting for a message.....");
            int recv = sock.ReceiveFrom(buffer, ref remoteEP);
            string message = Encoding.ASCII.GetString(buffer, 0, recv);
            Console.WriteLine("Received message: " + message + "\n");
            Message msg = JsonSerializer.Deserialize<Message>(message);
            switch (msg.Type)
            {
                case MessageType.Hello:
                    HandleHello(msg);
                    break;
                case MessageType.RequestData:
                    SendData();
                    break;
                case MessageType.Ack:
                    Console.WriteLine(msg.Content);
                    HandleAck(msg.Content);
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
    public void HandleHello(Message msg)
    {
        Console.WriteLine("Hello message received\n");

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
        Console.WriteLine("Send welcome message\n");
    }




    public void SendData(int ChunckToSend = 0)
    {
        string filePath = "hamlet.txt";
        string[] lines = File.ReadAllLines(filePath);
        //get all the lines into a array


        int chunkSize = 100; // Define the size of each message chunk

        int totalChunks = (int)Math.Ceiling((double)lines.Length / chunkSize); // get the amount of chunks depending on chucksize

        if (ChunckToSend <= totalChunks)
        {
            string MessageID = 000 + ChunckToSend.ToString("D3");//append the messageID in numbers of 4: 0001, 0002, 0003 etc.
            int start = ChunckToSend * chunkSize;//get the start of the chunk
            int end = Math.Min(start + chunkSize, lines.Length);//get the end
            List<string> MessageContent = lines.Skip(start).Take(end - start).ToList();//make the array into 1 string
            Console.WriteLine("Message Content:");

            Message data = new Message
            {
                Type = MessageType.Data,
                Content = MessageID + "" + string.Join("\n", MessageContent)
            };
            Console.WriteLine(data.Content);

            SendAck(data, MessageID);
        }
        else
        {
            SendEnd();
        }



    }

    //TODO: [End sending data to client]
    public void SendEnd()
    {
        Console.WriteLine("Send end message\n");
        // Perform any necessary cleanup or finalization here
        // For example, close the socket or release any resources
        Message msg = new Message();
        msg.Type = MessageType.End;
        byte[] msgBytes = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(msg));
        sock.SendTo(msgBytes, remoteEP);
    }

    public void HandleAck(string msgID)
    {
        Console.WriteLine("Ack message received with message ID: " + msgID);
        int NextChunk = int.Parse(msgID);
        NextChunk++;
        SendData(NextChunk);

    }

    public void SendAck(Message msg, string MessageID)
    {
        byte[] msgBytes = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(msg));
        try
        {
            sock.SendTo(msgBytes, remoteEP);
            Console.WriteLine("Data message sent ID: " + MessageID);
            Console.WriteLine("Waiting for Acknowledgement...");
            ReceiveMessage();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error sending data message: " + ex.Message);
            //handleerror
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