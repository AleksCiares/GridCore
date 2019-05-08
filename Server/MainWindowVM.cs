using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Server
{
    public class MainWindowVM : ViewModelBase
    {
        public MainWindowVM()
        {
            GRIDCore.initialize();

            _hostName = GRIDCore.HostNameGc;
            _hostIP = GRIDCore.HostIpGc;
            _hostPort = GRIDCore.HostPortGc;

            _realGridCore = new Thread(new ThreadStart(realizeGridCore));
            _realGridCore.Start();

            _waitSock = new Thread(GRIDCore.waitSocket);
            _waitSock.Start();
        }

        //public async void doSmth()
        //{
        //    await Task.Run(() => realizeGridCore());
        //}

        private void realizeGridCore()
        {
            while (true)
            {
                HostStatus = GRIDCore.HostStatusGc;

                Thread.Sleep(1000);

                Console.WriteLine(_hostStatus);

            }
        }

        public string HostName{ get { return _hostName; } }
        public string HostIP { get { return _hostIP; } }
        public int    HostPort { get { return _hostPort; } }
        public string HostStatus
        {
            get { return _hostStatus; }
            set
            {
                _hostStatus = value;
                RaisePropertyChanged(() => HostStatus);
            }
        }

        private string _hostName;
        private int    _hostPort;
        private string _hostIP;
        private string _hostStatus;
        private Thread _realGridCore;
        private Thread _waitSock;
        private static List<Socket> _sockets = new List<Socket>();
    }
}
