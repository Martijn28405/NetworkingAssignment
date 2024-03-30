using System.ComponentModel;
using System.Net;
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
         byte[] buffer = new byte[1000];
        byte[] msg = Encoding.ASCII.GetBytes("Hello from client\n");
        Socket sock;
        IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
        IPEndPoint ServerEndpoint = new IPEndPoint(ipAddress,32000);
        IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
        EndPoint remoteEP = (EndPoint)sender;
    try
    {
        sock = new Socket(AddressFamily.InterNetwork,
        SocketType.Dgram, ProtocolType.Udp);
        sock.SendTo(msg, msg.Length,
        SocketFlags.None, ServerEndpoint);
        int b = sock.ReceiveFrom(buffer, ref remoteEP);
        string data = Encoding.ASCII.GetString(buffer, 0, b);
        Console.WriteLine("Server said:" +  data);
        sock.Close();
    }
    catch
    {
    Console.WriteLine("\n Socket Error. Terminating");
    }
    }
    //TODO: create all needed objects for your sockets 

    //TODO: [Send Hello message]

    //TODO: [Receive Welcome]

    //TODO: [Send RequestData]

    //TODO: [Receive Data]

    //TODO: [Send RequestData]

    //TODO: [Send End]

    //TODO: [Handle Errors]


}