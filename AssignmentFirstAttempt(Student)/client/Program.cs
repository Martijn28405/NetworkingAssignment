using System.ComponentModel;
using System.Net;
using System.Net.NetworkInformation;
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

    //TODO: implement all necessary logic to create sockets and handle incoming messages
    // Do not put all the logic into one method. Create multiple methods to handle different tasks.
    public void start()
    {
        SendHelloMessage();
        ReceiveMessage();
    }
    //TODO: create all needed objects for your sockets ✓
    private Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
    private static byte[] buffer = new byte[66000];
    private static IPAddress ipAddress = NetworkInterface.GetAllNetworkInterfaces()
        .SelectMany(nic => nic.GetIPProperties().UnicastAddresses)
        .Where(ua => ua.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ua.Address))
        .Select(ua => ua.Address)
        .FirstOrDefault() ?? IPAddress.None;
    private static IPEndPoint ServerEndpoint = new IPEndPoint(ipAddress, 32000);
    private static EndPoint remoteEP = new IPEndPoint(ipAddress, 32000);

    private int threshold = 20;


    // recieved messages
    private SortedDictionary<string, string> recievedData = new SortedDictionary<string, string>();

    //TODO: [Send Hello message] ✓
    private void SendHelloMessage()
    {
        Message Hello = new Message
        {
            Type = MessageType.Hello,
            Content = $"{threshold}"
        };
        byte[] bytes = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(Hello));
        s.SendTo(bytes, ServerEndpoint);
    }

    private void ReceiveMessage()
    {
        while (true)
        {
            Console.WriteLine("Awaiting message...");
            
            try
            {
                s.ReceiveTimeout = 5000;
                int recv = s.ReceiveFrom(buffer, ref remoteEP);
                string message = Encoding.ASCII.GetString(buffer, 0, recv);
                Message msg = JsonSerializer.Deserialize<Message>(message)!;
                switch (msg.Type)
                {
                    case MessageType.Welcome:
                        Console.WriteLine("Welcome message received\nSending RequestData message\n");
                        SendRequestData();
                        break;

                    case MessageType.Data:
                        if (msg.Content != null)
                        {
                            Console.WriteLine(msg.Content.Split(" ").Length);
                            RecieveData(msg.Content);
                        }
                        break;

                    case MessageType.End:
                        HandleEnd();
                        break;
                }
                
            }
            catch (SocketException e)
            {
                if (e.SocketErrorCode == SocketError.TimedOut)
                {
                    HandleError();
                    break;
                }
                else
                {
                    throw;
                }
            }
        }
    }

    //TODO: [Send RequestData]
    private void SendRequestData()
    {
        Message DataRequest = new Message
        {
            Type = MessageType.RequestData,
            Content = "hamlet.txt"
        };
        byte[] data = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(DataRequest));
        try
        {
            s.SendTo(data, ServerEndpoint);
            Console.WriteLine("RequestData message sent to the server");
        }
        catch (SocketException)
        {
            HandleError();
        }
    }

    //TODO: [Receive Data]
    private void RecieveData(string messageContent){        
        try
        {
            string messageID = messageContent.Substring(0, 4);
            string messageData = messageContent.Substring(4);
            Console.WriteLine($"Data message received with Id: {messageID}");
            if (messageID == "0001")
            {
                File.Delete("output.txt");
            }
            recievedData[messageID] = messageData;
            SendAck(messageID);
        }
        catch
        {
            Console.WriteLine("Error: message is empty or faulty");
            //handleerror
        }

    }

    private void SendAck(string messageid){
        Message ackMessage = new Message{
            Type = MessageType.Ack,
            Content = messageid
        };
        byte[] msgBytes = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(ackMessage));
        try{
            s.SendTo(msgBytes, ServerEndpoint);
            Console.WriteLine("Ack message sent to the server");
        }
        catch(SocketException){
            HandleError();
        }
    }

    //TODO: [Recieve End]
    private void HandleEnd()
    {
        Console.WriteLine("End message received");
        // Perform any necessary cleanup or finalization here
        // For example, close the socket or release any resources
        if (s != null && s.Connected)
        {   
            Console.WriteLine("Closing socket");
            s.Close();
        }
        //Making the output file
        Console.WriteLine("Making output.txt");
        foreach (var data in recievedData)
        {
            File.AppendAllText("output.txt", data.Value);
        }
        // Terminate the application
        Console.WriteLine("Terminating client...");
        Environment.Exit(0);
    }


    //TODO: [Handle Errors]
    private void HandleError()
    {
        Environment.Exit(1);
    }
}