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

    //TODO: implement all necessary logic to create sockets and handle incoming messages
    // Do not put all the logic into one method. Create multiple methods to handle different tasks.
    public void start()
    {

    byte[] buffer = new byte[1000];
    byte[] msg = Encoding.ASCII.GetBytes(" From server: Your message delivered\n");
    string data = null;
    Socket sock;
    int MsgCounter = 0;
    IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
    IPEndPoint localEndpoint = new IPEndPoint(ipAddress, 32000);
    IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
    EndPoint remoteEP = (EndPoint)sender;
    try
        {
        sock = new Socket(AddressFamily.InterNetwork,
        SocketType.Dgram, ProtocolType.Udp);
        sock.Bind(localEndpoint);
        while (MsgCounter < 10)
        {
        Console.WriteLine("\n Waiting for the next client message..");
        int b = sock.ReceiveFrom(buffer, ref remoteEP);
        data = Encoding.ASCII.GetString(buffer, 0, b);
        Console.WriteLine("A message received from "+ remoteEP.ToString()+ " " + data);
        sock.SendTo(msg, msg.Length, SocketFlags.None, remoteEP);
        MsgCounter++;
        }
        sock.Close();
        }
    catch
        {
        Console.WriteLine("\n Socket Error. Terminating");
        }

    }

    //TODO: create all needed objects for your sockets 

    //TODO: keep receiving messages from clients
    // you can call a dedicated method to handle each received type of messages

    //TODO: [Receive Hello]

    //TODO: [Send Welcome]

    //TODO: [Receive RequestData]

    //TODO: [Send Data]

    //TODO: [Implement your slow-start algorithm considering the threshold] 

    //TODO: [End sending data to client]

    //TODO: [Handle Errors]

    //TODO: create all needed methods to handle incoming messages


}