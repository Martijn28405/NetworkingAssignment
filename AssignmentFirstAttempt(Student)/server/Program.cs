using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using MessageNS;

// Martijn Verwoert 1049334
// Karsten Keemink  1039658


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
    //TODO: create all needed objects for your sockets
    //this also creates the needed file systems and sets the IP adress with the port
    private byte[] buffer = new byte[1024];
    private static IPAddress ipAddress = NetworkInterface.GetAllNetworkInterfaces()
        .Where(nic => nic.OperationalStatus == OperationalStatus.Up)
        .SelectMany(nic => nic.GetIPProperties().UnicastAddresses)
        .Where(ua => ua.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ua.Address))
        .Select(ua => ua.Address)
        .FirstOrDefault() 
        ?? 
        IPAddress.Parse("127.0.0.1");

    // if the ip doesnt work somehow do:
    //private static IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
    private static IPEndPoint serverIpEndPoint = new IPEndPoint(ipAddress, 32000);
    private EndPoint remoteEP = new IPEndPoint(ipAddress, 32000);
    private Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

    //Vars for connection handling
    private bool Communicating = false;
    //------------------------------------------------

    //Vars for slow start
    private int Threshold = 1;
    private int chunksSize = 500;
    private string filePath = "";
    private string[] fileLines = new string[0];
    private Dictionary<string, string>? dataChunks;
    //------------------------------------------------


    // Vars for Ack handling
    private List<string> receivedAcks = new List<string>();
    
    List<string> sentMessages = new List<string>();
    //------------------------------------------------

    //this method binds the socket and lets the server listen
    public void start()
    {
        sock.Bind(serverIpEndPoint);
        while(true){
            try
            {
                HandleIncoming();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
        }
    }

    private DateTime lastReceivedTime = DateTime.Now; //set a time to compare to for Long timeout
    //TODO: keep receiving messages from clients
    // you can call a dedicated method to handle each received type of messages
    //this method lets the server listen for messages and handles the connection
    public void HandleIncoming()
    {
        
        try
        {
            Console.WriteLine($"\nserver is waiting for connection on port: {serverIpEndPoint} ...");
            
            while (true)
            {

                // Console.WriteLine($"\nlast recieved message at {lastReceivedTime}");
                // Console.WriteLine($"time now: {DateTime.Now}\n"); This was to check if the 5 sec timeout worked.
                if (Communicating &&lastReceivedTime != DateTime.MinValue && (DateTime.Now - lastReceivedTime).TotalSeconds > 5)
                {
                    Communicating = false;
                    HandleError(new Exception("No message received from client in 5 seconds"));
                    break;
                }

                try
                {
                    if(sentMessages.Count > 0){
                        if(sentMessages.Count == receivedAcks.Count){
                            Console.WriteLine("Server: All expected acks messages received");
                            break;
                        }
                    }
                    int received = sock.ReceiveFrom(buffer, ref remoteEP);
                    string message = Encoding.ASCII.GetString(buffer, 0, received);
                    Message decryptedMessage = JsonSerializer.Deserialize<Message>(message)!;
                    

                    switch (decryptedMessage.Type)
                    {
                        case MessageType.Hello:
                            if (!Communicating)
                            {
                                lastReceivedTime = DateTime.Now;
                                if (!int.TryParse(decryptedMessage.Content, out int threshold))
                                {
                                    HandleError(new FormatException($"Threshold could not be converted or is null"), true);
                                }
                                else
                                {
                                    Threshold = threshold;
                                    SendWelcome();
                                }
                            }
                            else{
                                lastReceivedTime = DateTime.Now;
                                break;
                            }
                            lastReceivedTime = DateTime.Now;
                            break;

                        case MessageType.RequestData:
                            lastReceivedTime = DateTime.Now;
                            if (decryptedMessage.Content == null || decryptedMessage.Content == ""){
                                HandleError(new Exception("Data message content is null or empty"), true);
                            }else{
                                if(sentMessages.Count > 0){
                                    HandleError(new Exception("Got a request for data while waiting for acks!"));
                                    break;
                                }else{
                                    ReceiveRequestData(decryptedMessage.Content);
                                }
                            }
                            break;

                        case MessageType.Ack:
                            lastReceivedTime = DateTime.Now;
                            if (decryptedMessage.Content != null && decryptedMessage.Content != "")
                            {
                                HandleAck(decryptedMessage.Content);
                                break;
                            }
                            else{
                                HandleError(new Exception("Ack message content is null or empty"), true);
                                break;
                            }                        
                        case MessageType.Error:
                            lastReceivedTime = DateTime.Now;
                            HandleError(new Exception(decryptedMessage.Content), false);
                            break;

                    }
                }
                catch (Exception ex) when (ex is not SocketException { SocketErrorCode: SocketError.TimedOut } && ex.Message != "Disconnecting client...")
                {
                    // Communicating = false;
                    HandleError(ex, false);
                }
            }
        }catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut){
            Console.WriteLine("Waiting time for acks is over!\n");
            return;
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
            Communicating = true;
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
        
        // Divide the file into chunks of size chunksSize
        dataChunks = DevideData(filePath, chunksSize);
        SendData();
    }

    private Dictionary<string, string> DevideData(string filepath, int chunksize, string startingDataID = "0001")
    {
        var dataDict = new Dictionary<string, string>();
        string dataID = startingDataID;
        string fileContent = File.ReadAllText(filepath);

        for (int i = 0; i <= fileContent.Length; i += chunksize)
        {
            int end = Math.Min(i + chunksize, fileContent.Length);
            string chunk = fileContent.Substring(i, end - i);
            dataDict.Add(dataID, chunk);
            int nextDataID = int.Parse(dataID) + 1;
            dataID = nextDataID.ToString("D4");
        }

        return dataDict;
    }
    //TODO: [Send Data]
    private void SendData()
    {
        Console.WriteLine($"Sending data...\n");
        // Initialize congestion window size to 1 (or any small value)
        int windowSize = 1;

        while (dataChunks != null && dataChunks.Count > 0)
        {

            List<KeyValuePair<string, string>> chunksList = dataChunks.ToList();
            for (int i = 0; i < windowSize && chunksList.Count > 0; i++)
            {   
                var chunk = chunksList.First();
                chunksList.RemoveAt(0);
                Message dataMessage = new Message
                {
                    Type = MessageType.Data,
                    Content = chunk.Key + "" + chunk.Value
                }; 
                byte[] messageBytes = Encoding.ASCII.GetBytes(JsonSerializer.Serialize(dataMessage));

                try
                {
                    sock.SendTo(messageBytes, remoteEP);
                    sentMessages.Add(chunk.Key);
                }
                catch (SocketException ex) 
                {
                    HandleError(ex);
                }
            }

            Console.WriteLine($"Sent messages: {string.Join(", ", sentMessages)}");

            sock.ReceiveTimeout = 1000;
            HandleIncoming();
            sock.ReceiveTimeout = 0;

            int ackIndex = 0;
            Console.WriteLine($"\nReceived acknowledgements:\n{string.Join(", ", receivedAcks)}.\n");
            while (ackIndex < receivedAcks.Count && receivedAcks[ackIndex] == sentMessages[ackIndex])
            {
                sentMessages.RemoveAt(ackIndex);
                dataChunks.Remove(receivedAcks[ackIndex]);
                receivedAcks.RemoveAt(ackIndex);
            }

            if(sentMessages.Count == 0){
                if (windowSize < Threshold)
                {
                    if(windowSize*2 > Threshold){
                        continue;
                    }else{
                        windowSize *= 2;
                    }
                }
            }else{
                Console.WriteLine($"Not received all acknowledgements...\nMissing ack: {sentMessages[0]}!");
                sentMessages.Clear();
                receivedAcks.Clear();
                windowSize = 1;
            }

        }
        Console.WriteLine("Sent all data!");
        Console.WriteLine("Received all acknowledgements");
        SendEnd();
    }

    private void HandleAck(string messageContent)
    {
        Console.WriteLine($"Ack message received with ID: {messageContent}\n");
        try{
            receivedAcks.Add(messageContent);
        }catch(Exception ex){
            HandleError(ex);
        }
        return;
    }

    //TODO: [Handle Errors]
    private void HandleError(Exception error, bool sendError = true, bool stopConnection = true)
    {
        if(error.Message == "No message received from client in 5 seconds"){
            error = new Exception("Client didnt respond in time.");
            Console.WriteLine("No message received from client in 5 seconds");
        }else{
            Console.WriteLine($"Error: {error.Message}\n");
        }
        
        
        if (sendError){
            Console.WriteLine($"Sending error message...\n");
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
        sentMessages.Clear();
        receivedAcks.Clear();
        dataChunks = null;
        Communicating = !stopConnection;
        sock.ReceiveTimeout = 0;
        if(stopConnection){
            throw new Exception("Disconnecting client...");
        }
        //HandleIncoming();
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
        }
        catch(SocketException ex){
            HandleError(ex);
        }
        Communicating = false;
        sock.ReceiveTimeout = 0;
        throw new Exception("Disconnecting client...");
        //HandleIncoming();
    }
}