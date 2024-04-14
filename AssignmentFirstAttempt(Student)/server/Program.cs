using System.Net;
using System.Net.Sockets;
using System.Reflection.Metadata;
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
     //TODO: create all needed objects for your sockets
        static UdpClient udpClient = new UdpClient(32000);
        static byte[] buffer = new byte[1024];
        static Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        static IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
        static IPEndPoint endPoint = new IPEndPoint(ipAddress, 32000);
       
    public void start()
    {
        Console.WriteLine("Server is listening on port 32000");
        HandleMessage(Encoding.ASCII.GetString(udpClient.Receive(ref endPoint)));
    }


        //TODO: keep receiving messages from clients
        // you can call a dedicated method to handle each received type of messages

        


        //TODO: [Send Data]

        //TODO: [Implement your slow-start algorithm considering the threshold] 

        //TODO: [End sending data to client]

        //TODO: [Handle Errors]

        //TODO: create all needed methods to handle incoming messages

    private static void SendWelcome()
    {
        // Create a Message object and set its properties
        Message welcomeMessage = new Message
        {
            Type = MessageType.Welcome,
            Content = "Welcome to the server!"
        };
    
        // Convert the Message object to a JSON string
        string json = JsonSerializer.Serialize(welcomeMessage);
    
        // Convert the JSON string to a byte array
        byte[] buffer = Encoding.ASCII.GetBytes(json);
    
        // Send the byte array to the client
        socket.SendTo(buffer, endPoint);
    }

    private void HandleMessage(string message)
    {

        while(true){
            // Do not put all the logic into one method. Create multiple methods to handle different tasks.
            // TODO: Implement logic to handle the received message
            switch ((MessageType)JsonSerializer.Deserialize<Message>(message).Type)
            {
                case MessageType.Hello:
              {
            // Handle the hello message
            // make sure the program continues and does not come in a loop 
            Console.WriteLine("Received hello message from client");

            // Send a welcome message to the client
            SendWelcome();
            HandleMessage(Encoding.ASCII.GetString(udpClient.Receive(ref endPoint)));
            break;
        }

                case MessageType.RequestData:
                    Console.WriteLine("Received request data from client");
                    SendData();
                    break;
                case MessageType.Data:
                Console.WriteLine("Received data from client");
                    break;
                case MessageType.End:
                    EndSendingData();
                    break;
                default:
                    HandleErrors();
                    break;
            }
        }
    }

    private void ReceiveRequestData(Socket socket)
    {
        //TODO: [Receive RequestData]
        // TODO: Implement logic to receive request data from the client
        byte[] buffer = new byte[1024];
        // TODO: Use the appropriate socket to receive the request data
        int bytesRead = socket.Receive(buffer);
        string requestData = Encoding.ASCII.GetString(buffer, 0, bytesRead);
        // TODO: Process the received request data
        Console.WriteLine("Received request data from client: " + requestData);
    }

    private void SendData()
    {
        // TODO: Implement logic to send data to the client
        string data = "This is the data to send";
        byte[] buffer = Encoding.ASCII.GetBytes(data);
        // TODO: Use the appropriate socket to send the data to the client
        Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
        IPEndPoint endPoint = new IPEndPoint(ipAddress, 32000);
        socket.SendTo(buffer, endPoint);
    }

    private void ImplementSlowStartAlgorithm()
    {
        // TODO: Implement logic for the slow-start algorithm considering the threshold
        int threshold = 10;
        int currentWindowSize = 1;
        // TODO: Implement the slow-start algorithm using the current window size and threshold
        while (currentWindowSize < threshold)
        {
            // Send data to the client
            SendData();
            currentWindowSize *= 2;
        }
    }

    private void EndSendingData()
    {
        // TODO: Implement logic to end sending data to the client
        // TODO: Use the appropriate socket to close the connection
        socket.Close();
    }

    private void HandleErrors()
    {
        // TODO: Implement logic to handle errors
        // TODO: Handle any exceptions or error conditions that may occur
    }
}