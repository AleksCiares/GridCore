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
        //  initialize IP host, IP address, end point & create socket-listner & bind it
        //   _ipHost = Dns.GetHostEntry("localhost");
        public static void Initialize()
        {
            while (true)
            {
                try
                {
                    HostStatusGc = Environment.NewLine + "------- GRID System was started at: " +
                        DateTime.Now + " -------";

                    Random random = new Random();
                    _hostPort = random.Next(1, 65000);
                    _ipHost = Dns.GetHostByName(Dns.GetHostName());
                    _ipAddr = _ipHost.AddressList[0];
                    _ipEndPoint = new IPEndPoint(_ipAddr, _hostPort);

                    _sockListner = new Socket(_ipAddr.AddressFamily, SocketType.Stream,
                        ProtocolType.Tcp);
                    _sockListner.Bind(_ipEndPoint); // связываем прослушивающий сокет с конечной точкой

                    if (_ipHost.AddressList[0].ToString() == "127.0.0.1")
                        HostStatusGc = "OFFLINE. Check internet connection...";
                    else
                        HostStatusGc = ($"ONLINE. Port: {_ipEndPoint.Port}. Waiting connection...");
                    
                    return;
                }
                catch (SocketException ex)
                {
                    HostStatusGc = ex.Message + " " + "(" + ex.Source + ")";
                    continue;
                }
                catch(Exception ex)
                {
                    HostStatusGc = ex.Message + " " + "(" + ex.Source + ")";
                    throw new Exception(ex.Message + " " + "(" + ex.Source + ")");
                }
            }
        }

        //  this method must be run in separate thread & you need call initialize() before
        public static void WaitSocket()
        {
            while (true)
            {
                try
                {
                    _sockListner.Listen(10);
                    while (true)
                    {
                        _sockets.Add(_sockListner.Accept());
                        //_sockets[_sockets.Count - 1].ReceiveTimeout = 5000;
                        HostStatusGc = ($"Socket was connected: #" + $"{_sockets.Count} " +
                            $"{_sockets[_sockets.Count - 1].AddressFamily.ToString()}");
                    }
                }
                catch(InvalidOperationException ex)
                {
                    HostStatusGc = ex.Message + " " + "(" + ex.Source + ")";
                    return;
                //    throw new Exception(ex.Message + " " + "(" + ex.Source + ")");
                }
                catch (Exception ex)
                {
                    HostStatusGc = ex.Message + " " + "(" + ex.Source + ")";
                    continue;
                }
            }
        }

        public static int GetLightyLoadedMachine(out Socket socket)
        {
            CheckConnection();
            if (_sockets.Count == 0)
                throw new Exception("No sockets for connection.\n");

            int workload = 100;
            int index = -1;
            int bytesRec = 0;

            byte[] msg =Encoding.Default.GetBytes(_WorkLoad);
            byte[] answer = new byte[56];
            
            for (int i = 0; i < _sockets.Count; i++)
            {
                _sockets[i].Send(msg);
                bytesRec = _sockets[i].Receive(answer);
                if (Int32.Parse(Encoding.Default.GetString(answer, 0, bytesRec)) < workload)
                {
                    index = i;
                    workload = Int32.Parse(Encoding.Default.GetString(answer, 0, bytesRec));
                }
                  
            }
            
            if (index == -1)
            {
                socket = _sockets[0];
                return index;
            }
            else
            {
                socket = _sockets[index];
                return index;
            }
        }

        // index of prepared socket to proccess data
        public static void SendDataToMachine(string filePath, string pathToDir, ref Socket socket)
        {
            CheckConnection();
            if (_sockets.Count == 0)
                throw new Exception("No sockets for connection\n");

            byte[] bytes = Encoding.Default.GetBytes(_ProcessData);
            socket.Send(bytes);

            bytes = new byte[2];
            int bytesRec = socket.Receive(bytes);
            if (Encoding.Default.GetString(bytes, 0, bytesRec) == "ok")
                Console.WriteLine(Encoding.Default.GetString(bytes, 0, bytesRec));

            //  если не будет работать попробовать Socket.ReceiveTimeout или Socket.ShutDown(SocketShutdown.Both)
            socket.SendFile(filePath);
            
            _file = new FileStream(pathToDir, FileMode.OpenOrCreate, FileAccess.ReadWrite,
                FileShare.None);

            int offset = 0;
            while (true)
            {
                bytes = new byte[1024];
                bytesRec = socket.Receive(bytes);

                if (bytesRec == 0)
                    break;

                _file.Write(bytes, 0, bytesRec);

                offset += bytesRec;

                if (socket.Available == 0)
                {
                    System.Threading.Thread.Sleep(1000);
                    if (socket.Available == 0)
                        break;
                }
                    
            }
  
            _file.Close();
        }

        public static void CheckConnection()
        {
            if (_sockets.Count == 0)
                return;

            for (int i = 0; i < _sockets.Count; i++)
            {
                if ((_sockets[i].Poll(1000, SelectMode.SelectRead) &&
                    (_sockets[i].Available == 0)) ||
                    !_sockets[i].Connected)
                {
                    HostStatusGc = ($"Disconect socket: #" + $"{i + 1} " +
                        $"{_sockets[i].RemoteEndPoint.AddressFamily.ToString()}");

                    _sockets[i].Shutdown(SocketShutdown.Both);
                    _sockets[i].Close();

                    _sockets.RemoveAt(i);
                }
            }
        }

        public static void TerminateGridCore()
        {
            if (_sockets.Count != 0)
            {
               
                do
                { 
                    _sockets[0].Shutdown(SocketShutdown.Both);
                    _sockets[0].Close();
                    _sockets.RemoveAt(0);
                } while (_sockets.Count != 0);

                //_sockListner.Shutdown(SocketShutdown.Both);
               // _sockListner.Disconnect(false);
            }

            _sockListner.Close();

            HostStatusGc = "------ GRID System was terminated at: " +
                DateTime.Now + " ------";
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
            set
            {
                lock (_locker)
                {
                    _hostStatus = value;

                    _logFile = new FileStream(@"GRID_LOG.txt", FileMode.OpenOrCreate, FileAccess.ReadWrite,
                           FileShare.None);
                    _logFile.Seek(0, SeekOrigin.End);

                    byte[] log = System.Text.Encoding.Default.GetBytes(_hostStatus + Environment.NewLine);
                    _logFile.Write(log, 0, log.Length);

                    _logFile.Close();
                }
            }
            get { return _hostStatus; }
        }

        private static IPHostEntry _ipHost;
        private static IPAddress   _ipAddr;
        private static IPEndPoint  _ipEndPoint;
        private static Socket      _sockListner;
        private static List <Socket>  _sockets = new List<Socket>();
        private static int _hostPort = 8005;
        private static String _hostStatus;
        private static FileStream _file;
        private static FileStream _logFile;
        
        private static string _WorkLoad = "load";
        private static string _ProcessData = "data";

        private static object _locker = new object();
    }
}
