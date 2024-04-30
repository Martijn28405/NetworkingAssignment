using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
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
    //TODO: implement all necessary logic to create sockets and handle incoming messages
    //TODO: create all needed objects for your sockets ✓

    private byte[] buffer = new byte[1024];
    private static IPAddress ipAddress = NetworkInterface.GetAllNetworkInterfaces()
        .SelectMany(nic => nic.GetIPProperties().UnicastAddresses)
        .Where(ua => ua.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ua.Address))
        .Select(ua => ua.Address)
        .FirstOrDefault() ?? IPAddress.None;
    private static IPEndPoint serverIpEndPoint = new IPEndPoint(ipAddress, 32000);
    private EndPoint remoteEP = new IPEndPoint(ipAddress, 32000);
    private Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
    
    // Is a client connected?
    private bool got_Connection = false;

    // further connection requests get put into a Queue
    private Queue<Message> connectionRequests = new Queue<Message>();

    //Data Info
    private int Threshold = 1;

    private string filePath = "";
    private string[] fileLines = new string[0];

    //Make Queue with chunks to send
    private Dictionary<string, string[]>? dataChunks;

    //acks recieving
    private List<string> sendMessages = new List<string>();


    // Do not put all the logic into one method. Create multiple methods to handle different tasks.
    public void start()
    {
        sock.Bind(serverIpEndPoint);
        RetrieveConnection();
    }

    //TODO: keep receiving messages from clients
    // you can call a dedicated method to handle each received type of messages
    public void RetrieveConnection()
    {
        Console.WriteLine();
        Console.WriteLine($"server is waiting for connection on port: {serverIpEndPoint} ...");

        while (!got_Connection)
        {
            try
            {
                if (connectionRequests.Count == 0){
                    // Receive message
                    int received = sock.ReceiveFrom(buffer, ref remoteEP);

                    // Handle message
                    string message = Encoding.ASCII.GetString(buffer, 0, received);
                    Console.WriteLine("Received message: " + message + "\n");
                    Message decryptedMessage = JsonSerializer.Deserialize<Message>(message)!;
                    // Handle message based on message type
                    HandleConnection(decryptedMessage);
                }else{
                    HandleConnection(connectionRequests.Dequeue());
                }
            }
            catch (Exception ex)
            {
                got_Connection = false;
                HandleError(ex);
            }
        }
        // Set a timeout for 5 seconds
        sock.ReceiveTimeout = 5000;
        
        try
        {
            while (got_Connection)
            {
                try{
                    // Receive message after establishing a connection
                    int requestRecieved = sock.ReceiveFrom(buffer, ref remoteEP);
                    // Handle message
                    string requestMessage = Encoding.ASCII.GetString(buffer, 0, requestRecieved);
                    Message decryptedRequestMessage = JsonSerializer.Deserialize<Message>(requestMessage)!;
            
                    switch (decryptedRequestMessage.Type)
                    {
                        case MessageType.RequestData:
                            if (decryptedRequestMessage.Content != null)
                            {
                                ReceiveRequestData(decryptedRequestMessage.Content);
                            }
                            else
                            {
                                HandleError(new Exception("Null message content received."));
                            }
                            return; // Exit the loop when a RequestData message is received
                        case MessageType.Hello:
                            Console.WriteLine("Received Hello message");
                            connectionRequests.Enqueue(decryptedRequestMessage); // Store the message in the queue to connect after ending this connection
                            break;
                        case MessageType.Error:
                            HandleError(new Exception(decryptedRequestMessage.Content), false);
                            break;
                        default:
                            HandleError(new Exception($"Unwanted message received: {decryptedRequestMessage}"));
                            break;
                    }
                }catch (Exception ex)
                {
                    // Log error
                    HandleError(ex);
                    // Go back to listening for a hello message

                    got_Connection = false;
                    continue;
                }

            }
        }
        catch (SocketException e) when (e.SocketErrorCode == SocketError.TimedOut)
        {
            // If no request data message is received within 5 seconds, call the SendEnd method
            SendEnd();
        }
    }

    private void HandleConnection(Message decryptedmessage)
    {
        try
        {
            switch (decryptedmessage.Type)
            {
                //TODO: [Receive Hello] ✓
                case MessageType.Hello:
                    if (int.TryParse(decryptedmessage.Content, out int threshold))
                    {
                        Threshold = threshold;
                        Console.WriteLine("Hello message received. Threshold: " + Threshold);
                        SendWelcome();
                    }
                    else
                    {
                        HandleError(new Exception("Invalid threshold value"));
                    }
                    break;
                case MessageType.Error:
                    HandleError(new Exception(decryptedmessage.Content), false);
                    break;
                default:
                    HandleError(new Exception("Invalid message type"));
                    break;
            }
        }
        catch (Exception ex)
        {
            HandleError(ex);
        }
    }

    //TODO: [Send Welcome] ✓
    private void SendWelcome()
    {
        Message welcomeMessage = new Message { Type = MessageType.Welcome };
        byte[] messageBytes = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(welcomeMessage));
        try
        {
            sock.SendTo(messageBytes, remoteEP);
            Console.WriteLine("Send welcome message\n");
            got_Connection = true;
        }
        catch (SocketException ex)
        {
            HandleError(ex);
        }
    }

    //TODO: [Receive RequestData]
    private void ReceiveRequestData(string messageContent)
    {
        Console.WriteLine($"Data requested: {messageContent}");
        filePath = messageContent;
        fileLines = File.ReadAllText(filePath).Split('\n');
        dataChunks = DevideData(fileLines);
        SendData();
    }

    private Dictionary<string, string[]> DevideData(string[] filelines)
    {
        var dataDict = new Dictionary<string, string[]>();
        int chunksize = 100;
        string messageID = "0001";

        for (int i = 0; i <= fileLines.Length; i += chunksize)
        {
            int end = Math.Min(i + chunksize, filelines.Length);
            string[] chunk = filelines.Skip(i).Take(end - i).ToArray();
            dataDict.Add(messageID, chunk);
            int nextMessageID = int.Parse(messageID) + 1;
            messageID = nextMessageID.ToString("D4");
        }

        return dataDict;
    }
    //TODO: [Send Data]
    private void SendData()
    {
        // Initialize congestion window size to 1 (or any small value)
        int windowSize = 1;
        sendMessages = new List<string>();

        // While there is data to send
        while (dataChunks != null && dataChunks.Count > 0)
        {
            // Create a queue to hold the chunks of data to be sent
            Queue<KeyValuePair<string, string[]>> chunksQueue = new Queue<KeyValuePair<string, string[]>>(dataChunks);
            
            for (int i = 0; i < windowSize && chunksQueue.Count > 0; i++)
            {
                // Get the next chunk of data to send
                var chunk = chunksQueue.Dequeue();
            
                // Create a message with the chunk data
                Message dataMessage = new Message
                {
                    Type = MessageType.Data,
                    Content = chunk.Key + "" + string.Join("", chunk.Value)
                };
            
                // Convert the message to bytes
                byte[] messageBytes = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(dataMessage));
            
                // Send the message
                try
                {
                    sock.SendTo(messageBytes, remoteEP);
                    sendMessages.Add(chunk.Key);
                }
                catch (SocketException ex) 
                {
                    HandleError(ex);
                }
            }
            Console.WriteLine($"Send messages with Id's:\n[{string.Join(", ", sendMessages)}]\n");

            // Wait for acknowledgements from the client
            sock.ReceiveTimeout = 1000;
            try{WaitForAcks();}
            catch(SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut){
                if (windowSize > 1){
                    windowSize /= 2;
                }
                HandleError(ex);
            }

            if (sendMessages.Count == 0)
            {
                // If all acknowledgements are received, increase the window size
                if (windowSize < Threshold)
                {
                    // Double the congestion window size
                    windowSize *= 2;
                    if(windowSize > Threshold){
                        windowSize = Threshold;
                    }
                }
            }
            else
            {
                // If a timeout or lost acknowledgement occurs, set the threshold to windowSize / 2, reset windowSize to the previous attempt, and resend the unacked messages
                Threshold = windowSize / 2;
                windowSize /= 2;
                sendMessages.Clear();
            }
        }
        Console.WriteLine("Send all data!");
        Console.WriteLine("Recieved all acknowledgements");
        SendEnd();
    }

    private void WaitForAcks()
    {
        while(sendMessages.Count > 0){
            // Receive ack message
            int received = sock.ReceiveFrom(buffer, ref remoteEP);

            // Handle ack message
            string message = Encoding.ASCII.GetString(buffer, 0, received);
            Message DecryptedMessage = JsonSerializer.Deserialize<Message>(message)!;
            Console.WriteLine($"Recieved {DecryptedMessage.Type} for messageID: {DecryptedMessage.Content}\n");
            switch (DecryptedMessage.Type)
            {
                case MessageType.Ack:
                    if (DecryptedMessage.Content != null)
                    {
                        sendMessages.Remove(DecryptedMessage.Content);
                        dataChunks?.Remove(DecryptedMessage.Content);
                    }
                    break;
                case MessageType.Hello:
                    connectionRequests.Enqueue(DecryptedMessage);
                    break;
                default:
                    HandleError(new Exception($"Unexpected message recieved: {DecryptedMessage}"));
                    break;
            }
        }
    }

    //TODO: [Handle Errors]
    private void HandleError(Exception error, bool sendError = true)
    {
        Console.WriteLine("Handle Error called");
        Console.WriteLine("Error: " + error.Message);
        if (sendError){
            try
            {
                Message errorMessage = new Message{
                    Type = MessageType.Error,
                    Content = error.Message
                };
                byte[] messageBytes = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(errorMessage));
                sock.SendTo(messageBytes, remoteEP);
            }
            catch
            {
                Console.WriteLine("Sending error faulted");
            }
        }

        sock.ReceiveTimeout = 0;
        RetrieveConnection();
    }

    //Send the end message and reset all the timeouts, also restart the connection on RetrieveConnection
    private void SendEnd(){
        Console.WriteLine("Sending end message...\n");
        Message endMessage = new Message{
            Type = MessageType.End
        };
        byte[] messageBytes = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(endMessage));
        try{
            sock.SendTo(messageBytes, remoteEP);
            got_Connection = false;
        }
        catch(SocketException ex){
            HandleError(ex);
        }
        sock.ReceiveTimeout = 0;
        RetrieveConnection();
    }
}