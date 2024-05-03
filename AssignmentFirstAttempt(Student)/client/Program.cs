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


    //TODO: create all needed objects for your sockets ✓
    private Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
    private static byte[] buffer = new byte[1024];
    private static IPAddress ipAddress = NetworkInterface.GetAllNetworkInterfaces()
        .Where(nic => nic.OperationalStatus == OperationalStatus.Up)
        .SelectMany(nic => nic.GetIPProperties().UnicastAddresses)
        .Where(ua => ua.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ua.Address))
        .Select(ua => ua.Address)
        .FirstOrDefault() ?? IPAddress.Parse("127.0.0.1");
        
    // if the ip doesnt work somehow do:
    //private static IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
    private static IPEndPoint ServerEndpoint = new IPEndPoint(ipAddress, 32000);
    private static EndPoint remoteEP = new IPEndPoint(ipAddress, 32000);

    private int threshold = 20;
    private string fileName = "hamlet.txt";

    // recieved messages
    private SortedDictionary<string, string> recievedData = new SortedDictionary<string, string>();

    //TODO: implement all necessary logic to create sockets and handle incoming messages
    // Do not put all the logic into one method. Create multiple methods to handle different tasks.
    public void start()
    {
        try{
            SendHelloMessage();
        }catch(SocketException ex){
            Console.WriteLine($"Error: {ex.Message}\n\n");
            HandleError(ex, false);
        }
    }

    //TODO: [Send Hello message] ✓
    private void SendHelloMessage()
    {
        Message Hello = new Message
        {
            Type = MessageType.Hello,
            Content = $"{threshold}"
        };
        byte[] bytes = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(Hello));
        Console.WriteLine($"Sending Hello message to: {ServerEndpoint}\n");
        try{
            s.SendTo(bytes, ServerEndpoint);
            ReceiveMessage();
        }
        catch(SocketException ex) 
        {
            Console.WriteLine(ex.ErrorCode + ""+ ex.SocketErrorCode);
            Console.WriteLine("Sending Hello message faulted");
            HandleError(ex, false);
        }
    }

    private void ReceiveMessage()
    {
        s.ReceiveTimeout = 5000;
        try{
            while (true)
            {
                Console.WriteLine("Awaiting message...");
                int recv = s.ReceiveFrom(buffer, ref remoteEP);
                string message = Encoding.ASCII.GetString(buffer, 0, recv);
                Message msg = JsonSerializer.Deserialize<Message>(message)!;
                switch (msg.Type)
                {
                    case MessageType.Welcome:
                        Console.WriteLine("Welcome message received\n");
                        SendRequestData();
                        break;

                    case MessageType.Data:
                        if (msg.Content != null)
                        {
                            RecieveData(msg.Content);
                        }else{
                            HandleError(new Exception("Data message content is null"));
                        }
                        break;
                    
                    case MessageType.Error:
                        Console.WriteLine("Error message received\n");
                        HandleError(new Exception(msg.Content), false);
                        break;

                    case MessageType.End:
                        HandleEnd();
                        break;
                    
                    default:
                        HandleError(new Exception($"Unknown message type: {msg.Type} with content:\n{msg.Content}"), false);
                        break;
                }
            }
        }catch(SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut || ex.SocketErrorCode == SocketError.MessageSize) {
            
            Console.WriteLine("Socket timed out");
            HandleError(ex, false); 
        }
    }

    //TODO: [Send RequestData]
    private void SendRequestData()
    {
        Message DataRequest = new Message
        {
            Type = MessageType.RequestData,
            Content = fileName
        };
        byte[] data = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(DataRequest));
        Console.WriteLine("Sending RequestData message\n");
        try
        {
            s.SendTo(data, ServerEndpoint);
        }
        catch (SocketException ex)
        {
            Console.WriteLine("Sending RequestData message faulted\n");
            HandleError(ex);
        }
    }
    // int count = 0;   //count for testing a missing ack and server long timeout
    //TODO: [Receive Data]
    private void RecieveData(string messageContent){      
        try
        {
            
            string messageID = messageContent.Substring(0, 4);
            string messageData = messageContent.Substring(4);
            Console.WriteLine($"\nData message received with Id: {messageID}");
            if (messageID == "0001")
            {
                File.Delete(fileName);
            }
            // if(count < 1 && messageID == "0245"){ //at first run not sending ack back //also test if nothing is send in 5 secs
            //     count++;
            //     count--;
            //     return;
            // }
            // else{
                recievedData[messageID] = messageData;
                SendAck(messageID);
            // }
        }
        catch(Exception ex)
        {
            HandleError(ex);
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
            Console.WriteLine("Ack message sent to the server\n");
        }
        catch(SocketException ex){
            Console.WriteLine("Ack message sent faulted\n");
            HandleError(ex);
        }
    }

    //TODO: [Recieve End]
    private void HandleEnd()
    {
        Console.WriteLine("End message received");
        // Perform any necessary cleanup or finalization here
        // For example, close the socket or release any resources
        if (s != null)
        {   
            Console.WriteLine("Closing socket");
            s.Close();
        }
        //Making the output file
        try
        {
            Console.WriteLine($"Making {fileName}");
            string fullFileData = string.Join("", recievedData.Values);
            File.WriteAllText(fileName, fullFileData);
        }
        catch(Exception ex)
        {
            Console.WriteLine("Making output.txt faulted\n");
            HandleError(ex);
        }
        // Terminate the application
        Console.WriteLine("Terminating client...");
        Environment.Exit(0);
    }


    //TODO: [Handle Errors]
    private void HandleError(Exception exception, bool sendError = true)
    {
        Console.WriteLine($"\nError: {exception.Message}\n");
        if (sendError){
            Console.WriteLine("Sending error message...");
            try
            {
                Message errorMessage = new Message{
                    Type = MessageType.Error,
                    Content = exception.Message
                };
                byte[] messageBytes = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(errorMessage));
                s.SendTo(messageBytes, ServerEndpoint);
            }
            catch
            {
                Console.WriteLine("Sending error message faulted");
            }
        }
        if (s != null)
        {
            Console.WriteLine("Closing socket");
            s.Close();
        }
        Console.WriteLine("Terminating client...");
        Environment.Exit(1);
    }
}