using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace TestClient
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                int port = Int32.Parse(Console.ReadLine());
                SendMessageFromSocket(port);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                Console.ReadLine();
            }
        }

        static void SendMessageFromSocket(int port)
        {
            IPHostEntry ipHost = Dns.GetHostByName(Dns.GetHostName());
            IPAddress ipAddress = ipHost.AddressList[0];
           // IPAddress[] ipAddress = Dns.GetHostAddresses(ip);
            IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, port);

            Console.WriteLine($"Name: {ipHost.HostName.ToString()}\n" +
                              $"IP address:{ipAddress.ToString()}\n" +
                              $"Port: {port}\n");

            Socket sender = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            sender.Connect(ipEndPoint);

            Console.WriteLine("Connected");

            string data = null;
            byte[] bytes = null;
            int bytesRec = 0;

            bytes  = new byte[256];
            bytesRec = sender.Receive(bytes);
            data = Encoding.Default.GetString(bytes, 0, bytesRec);
            Console.WriteLine(data);

            bytes = Encoding.Default.GetBytes("10");
            sender.Send(bytes);

            bytes = new byte[256];
            bytesRec = sender.Receive(bytes);
            data = Encoding.Default.GetString(bytes, 0, bytesRec);
            Console.WriteLine(data);

            bytes = Encoding.Default.GetBytes("ready");
            sender.Send(bytes);

            FileStream file = new FileStream("F:\\new.txt", FileMode.OpenOrCreate, FileAccess.ReadWrite,
                FileShare.None);

            int offset = 0;
            while(sender.Available != 0)
            {
                bytes = new byte[1024];
                bytesRec = sender.Receive(bytes);

                if (bytesRec == 0)
                    break;

                file.Write(bytes, offset, bytesRec);

                offset += bytesRec;
            }

            file.Close();

            Console.ReadLine();
            sender.Shutdown(SocketShutdown.Both);
            sender.Close();
        }
    }
}
