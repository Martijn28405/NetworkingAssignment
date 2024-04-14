using System;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using MessageNS;


class Program{
    static void Main(string[] args){
        ClientUDP cUDP = new ClientUDP();
        cUDP.Start();
    }

}
class ClientUDP
{
    private UdpClient udpClient;
    private IPEndPoint serverEndPoint;

    public void Start()
    {
        udpClient = new UdpClient();
        serverEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 32000);

        SendHello();
    }

    private void SendHello()
    {
        try
        {
            Message helloMessage = new Message
            {
                Type = MessageType.Hello,
                Content = "Hello"
            };
            byte[] helloBytes = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(helloMessage));
            udpClient.Send(helloBytes, helloBytes.Length, serverEndPoint);
            Console.WriteLine($"Sent hello message");
            ReceiveWelcome();
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"Error sending hello message: {ex.Message}");
        }
    }

    private void ReceiveWelcome()
    {
        try
        {
            byte[] welcomeBytes = udpClient.Receive(ref serverEndPoint);
            string welcomeMessage = Encoding.ASCII.GetString(welcomeBytes);
            Message welcome = JsonSerializer.Deserialize<Message>(welcomeMessage);
            if (welcome.Type == MessageType.Welcome)
            {
            Console.WriteLine($"Received welcome message: {welcome.Content}");
            SendRequestData();
            }
            else
            {
            Console.WriteLine($"Received unexpected message type: {welcome.Type}");
            }
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"Error receiving welcome message: {ex.Message}");
        }

    }

    private void SendRequestData()
    {
        try
        {
            Message requestDataMessage = new Message
            {
                Type = MessageType.RequestData,
                Content = "Request data"
            };
            byte[] requestDataBytes = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(requestDataMessage));
            udpClient.Send(requestDataBytes, requestDataBytes.Length, serverEndPoint);
            Console.WriteLine($"Sent request data message");
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"Error sending request data message: {ex.Message}");
        }
    }

    private void ReceiveData()
    {
        try
        {
            byte[] dataBytes = udpClient.Receive(ref serverEndPoint);
            string dataMessage = Encoding.ASCII.GetString(dataBytes);
            Message data = JsonSerializer.Deserialize<Message>(dataMessage);
            Console.WriteLine($"Received data: {data.Content}");
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"Error receiving data: {ex.Message}");
        }
    }

    private void SendEnd()
    {
        try
        {
            Message endMessage = new Message
            {
                Type = MessageType.End,
                Content = "End"
            };
            byte[] endBytes = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(endMessage));
            udpClient.Send(endBytes, endBytes.Length, serverEndPoint);
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"Error sending end message: {ex.Message}");
        }
    }
}

    //TODO: Implement error handling logic

    //TODO: Implement error handling logic