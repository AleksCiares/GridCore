using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.IO;

namespace Server
{
    public static class GRIDCore 
    {
       //   initialize IP host, IP address, end point & create socket-listner & bind it
        public static void initialize()
        {
                _ipHost = Dns.GetHostEntry(Dns.GetHostName());
                _ipAddr = _ipHost.AddressList[1];
                _ipEndPoint = new IPEndPoint(_ipAddr, _hostPort);

                _sockListner = new Socket(_ipAddr.AddressFamily, SocketType.Stream,
                    ProtocolType.Tcp);

                _sockListner.Bind(_ipEndPoint); // связываем прослушивающий сокет с конечной точкой
        }

        //   this method must be run in separate thread
        public static void waitSocket()
        {
            try
            {
                _sockListner.Listen(10);

                while (true)
                {
                    _hostStatus = ($"Port: {_ipEndPoint.Port}. Waiting connection...");

                    _sockets.Add(_sockListner.Accept());

                    _hostStatus = ($"Connected socket: {_sockets.Count}");
                }
            }
            catch (Exception ex)
            {
                _hostStatus = ex.ToString();
            }
        }

        public static void closeSockets()
        {
            for(int i =0;  i < _sockets.Count; i++)
            {
                _sockets[i].Shutdown(SocketShutdown.Both);
                _sockets[i].Close();
            }

            _objCount--;
        }

        public static ref Socket getLightyLoadedMachine(ref Socket socket)
        {
            int workload = 100;
            int index = -1;
            byte[] msg = Encoding.UTF8.GetBytes(Commands.Workload.ToString());
            byte[] answer = new byte[10];

            for(int i = 0; i < _sockets.Count; i ++)
            {
                _sockets[i].Send(msg);

                _sockets[i].Receive(answer);
                if (Int32.Parse(Encoding.UTF8.GetString(answer)) < workload)
                {
                    index = i;
                    socket = _sockets[i];
                }
            }

            if (index == -1)
            {
                socket = _sockets[0];
                return ref socket;
            }
            else
                return ref socket;
        }
        


        // index of prepared socket to proccess data
        public static void proccessData(string filePath, int index)
        {
            //  если не будет работать попробовать Socket.ReceiveTimeout или Socket.ShutDown(SocketShutdown.Both)
            _sockets[index].SendFile(filePath);

            _file = new FileStream("newFile.txt", FileMode.CreateNew, FileAccess.ReadWrite,
                FileShare.None);

            while (true)
            {
                if(_sockets[index].Available > 4096)
                {
                    byte[] data = new byte[4096];
                    int recv = _sockets[index].Receive(data, SocketFlags.None);

                    if (recv == 0)
                        break;

                    _file.Write(data, 0, recv);
                }
                //  иначе проверяем, является ли это последним блоком данных, который меньше нашего буфера
                else
                {
                    if(_file.Length + _sockets[index].Available == 5402624)
                    {
                        byte[] data = new byte[4096];
                        int recv = _sockets[index].Receive(data, SocketFlags.None);

                        _file.Write(data, 0, recv);

                        break;
                    }
                }
            }
            // если нет подходящих условий - то ничего не делаем, пока собственно буфер приема не заполнится
        }
        
        public static string HostNameGc
        {
            get { return _ipHost.HostName.ToString(); }
        }

        public static int HostPortGc
        {
            get { return _hostPort;   }
        }

        public static string HostIpGc
        {
            get { return _ipAddr.ToString(); }
        }

        public static string HostStatusGc
        {
            get { return _hostStatus; }
        }

        private static IPHostEntry _ipHost;
        private static IPAddress   _ipAddr;
        private static IPEndPoint  _ipEndPoint;
        private static Socket      _sockListner;
        private static List <Socket>  _sockets = new List<Socket>();
        private static int _hostPort = 0;
        private static String _hostStatus;
        private static FileStream _file;
        private static int _objCount = 0;

        private enum Commands
        {
           Workload = 0001010,
           LoadData
        }
    }
}
