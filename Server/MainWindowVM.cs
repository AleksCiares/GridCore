using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using System;
using System.ComponentModel;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Server
{
    public class MainWindowVM : ViewModelBase
    {
        public MainWindowVM()
        {
            try
            {
                GRIDCore.Initialize();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message + " " + "(" + ex.Source + ")" + 
                    "\nError of host initializing. Reload the GRID System");
                GRIDCore.TerminateGridCore();
            }

            _hostName = GRIDCore.HostNameGc;
            _hostIP = GRIDCore.HostIpGc;
            _hostPort = GRIDCore.HostPortGc;
            _hostStatus = GRIDCore.HostStatusGc;

            _waitSockThread = new Thread(GRIDCore.WaitSocket);
            _waitSockThread.Start();
        }

        private void CheckAndSendData()
        {
            if(_pathToDir == null || _pathToFile == null)
            {
                MessageBox.Show("Incorrect path to processed files or file.\n");
                return;
            }

            try
            {
                Socket socket;
                if (GRIDCore.GetLightyLoadedMachine(out socket) == -1)
                {
                    MessageBox.Show("No unloaded machines.\n");
                    return;
                }

                string pathToProcessedFile = _pathToDir + @"\Processed_File_" + processedFileCount.ToString() + ".txt";
                GRIDCore.SendDataToMachine(_pathToFile, pathToProcessedFile, ref socket);

                processedFileCount++;
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message + " " + "(" + ex.Source + ")" +
                    "\nTry again when remote machines are ready.");
                return;
            }
        }

        public ICommand SendDataToMachine
        {
            get
            {
                return _loadFileDir ?? (_loadFileDir = new RelayCommand(() =>
                {
                    Task.Factory.StartNew(() =>
                    {
                        CheckAndSendData();
                    });
                }));
            }
        }

        public ICommand WindowClosing
        {
            get
            {
                return new RelayCommand<CancelEventArgs>(
                    (args) => {
                        MessageBox.Show("Closing Grid System ;(");
                        GRIDCore.TerminateGridCore();
                    });
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

        public string PathToDir
        {
            get { return _pathToDir;  }
            set
            {
                _pathToDir = @"" + value;
                if (_pathToDir == null)
                {
                    _validDirectory = false;
                    return;
                }

                try
                {
                    DirectoryInfo dirInfo = new DirectoryInfo(_pathToDir);
                    if (!dirInfo.Exists)
                        dirInfo.Create();
                    
                    ValidDirectory = true;
                }
                catch(ArgumentException ex)
                {
                    MessageBox.Show("path contains invalid characters such as \", <, >, or |.\n" + 
                        ex.Message);
                    _pathToDir = null;
                    ValidDirectory = false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("The directory cannot be created.\n" +
                        ex.Message);
                    _pathToDir = null;
                    ValidDirectory = false;
                }
            }
        }
        public bool ValidDirectory
        {
            get { return _validDirectory; }
            set
            {
                _validDirectory = value;
                RaisePropertyChanged(() => ValidDirectory);
            }
        }

        public string PathToFile
        {
            get { return _pathToFile; }
            set
            {
                _pathToFile = @"" + value;
                if (_pathToFile == null)
                {
                    ValidPathFile = false;
                    return;
                }

                try
                {
                    FileInfo fileInfo = new FileInfo(_pathToFile);
                    if(!fileInfo.Exists)
                    {
                        MessageBox.Show("File not exist.");
                        _pathToFile = null;
                        ValidPathFile = false;

                        return;
                    }
                    
                    ValidPathFile = true;
                }
                catch (ArgumentException ex)
                {
                    MessageBox.Show("path contains invalid characters such as \", <, >, or |.\n" +
                        ex.Message);
                    _pathToDir = null;
                    ValidDirectory = false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    _pathToFile = null;
                    ValidPathFile = false;
                }
            }
        }
        public bool ValidPathFile
        {
            get { return _validPathFile; }
            set
            {
                _validPathFile = value;
                RaisePropertyChanged(() => ValidPathFile);
            }
        }

        private string _hostName;
        private int    _hostPort;
        private string _hostIP;
        private string _hostStatus;

        private string _pathToDir;
        private string _pathToFile;
        private bool _validDirectory = false;
        private bool _validPathFile = false;
        private int processedFileCount = 0;

        private Thread _waitSockThread;

        private ICommand _loadFileDir;        
    }
}
