using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace mPlayer.Model
{
    public class SharingModel : BindableObject
    {
        #region Variables

        private Thread ListenThread;
        private TcpListener listener;

        #endregion

        #region Properties

        private List<FileItem> navigationList;
        public List<FileItem> NavigationList
        {
            get { return navigationList; }
            set
            {
                navigationList = value;
                OnPropertyChanged("NavigationList");
            }
        }

        private double _totalProgress;
        public double TotalProgress
        {
            get { return _totalProgress; }
            set
            {
                _totalProgress = value;
                OnPropertyChanged("TotalProgress");
            }
        }

        private int _progress;
        public int Progress
        {
            get { return _progress; }
            set
            {
                if (_progress != value)
                {
                    _progress = value;
                    OnPropertyChanged("Progress");
                }
            }
        }

        private string _currentFile;
        public string CurrentFile
        {
            get { return _currentFile; }
            set
            {
                _currentFile = value;
                OnPropertyChanged("CurrentFile");
            }
        }

        private bool _isDownloading;
        public bool IsDownloading
        {
            get { return _isDownloading; }
            set
            {
                _isDownloading = value;
                OnPropertyChanged("IsDownloading");
            }
        }

        private bool _isConnected;
        public bool IsConnected
        {
            get { return _isConnected; }
            set
            {
                _isConnected = value;
                OnPropertyChanged("IsConnected");
            }
        }

        public bool IsCanceled
        { get; set; }

        private string _localAddress;
        public string LocalAddress
        {
            get { return _localAddress; }
            set
            {
                _localAddress = value;
                OnPropertyChanged("LocalAddress");
            }
        }

        private string _address;
        public string Address
        {
            get { return _address; }
            set
            {
                _address = value;
                OnPropertyChanged("Address");
            }
        }

        #endregion

        #region Class

        public class FileItem : BindableObject
        {
            public bool IsFolder
            { get; set; }

            public string Name
            { get; set; }

            public long Size
            { get; set; }

            public string Path
            { get; set; }

            private bool _isChecked;
            [JsonIgnore()]
            public bool IsChecked
            {
                get { return _isChecked; }
                set
                {
                    _isChecked = value;
                    OnPropertyChanged("IsChecked");
                }
            }
        }

        #endregion

        public SharingModel(bool start)
        {
            if (start)
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        LocalAddress = ip.ToString();
                    }
                }

                ListenThread = new Thread(() =>
                {
                    byte[] buffer = new byte[4096];

                    try
                    {
                        listener = new TcpListener(IPAddress.Any, 8080);
                        listener.Start();
                    }
                    catch
                    {
                        return;
                    }

                    try
                    {
                        while (true)
                        {
                            using (var client = listener.AcceptTcpClient())
                            {
                                using (var ns = client.GetStream())
                                {
                                    int bytesRead = ns.Read(buffer, 0, buffer.Length);
                                    var msg = Encoding.Default.GetString(buffer, 0, bytesRead);
                                    var query = msg.Substring(5);

                                    // Command is like: #### <query>
                                    // #### -> LIST, SEND
                                    if (msg.StartsWith("LIST"))
                                    {
                                        var list = new List<FileItem>();

                                        if (string.IsNullOrEmpty(query))
                                        {
                                            foreach (var i in Directory.GetLogicalDrives())
                                            {
                                                list.Add(new FileItem() { IsFolder = true, Name = i, Path = i });
                                            }
                                        }
                                        else
                                        {
                                            list.Add(new FileItem() { IsFolder = true, Name = "..", Path = Path.GetDirectoryName(query) });

                                            try
                                            {
                                                foreach (var i in Directory.GetDirectories(query))
                                                {
                                                    list.Add(new FileItem() { IsFolder = true, Name = Path.GetFileName(i), Path = i });
                                                }

                                                foreach (var i in Directory.GetFiles(query))
                                                {
                                                    list.Add(new FileItem() { IsFolder = false, Name = Path.GetFileName(i), Path = i, Size = new FileInfo(i).Length });
                                                }
                                            }
                                            catch
                                            {
                                            }
                                        }

                                        var json = JsonConvert.SerializeObject(list);
                                        var buf = Encoding.Default.GetBytes(json);

                                        ns.Write(buf, 0, buf.Length);
                                        ns.Flush();
                                    }
                                    else if (msg.StartsWith("SEND"))
                                    {
                                        var fs = File.OpenRead(query);
                                        bytesRead = 0;

                                        while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                                        {
                                            try
                                            {
                                                ns.Write(buffer, 0, bytesRead);
                                                ns.Flush();
                                            }
                                            catch { break; }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {

                    }

                });
                ListenThread.Start();
            }
        }

        public void Connect()
        {
            IsConnected = true;
            SendMessage(@"LIST ");
        }

        public void Close()
        {
            if (listener != null)
            {
                listener.Stop();
            }
        }

        public void SendMessage(string cmd)
        {
            using (var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                try
                {
                    client.Connect(Address, 8080);

                    using (var ns = new NetworkStream(client))
                    {
                        byte[] buffer = Encoding.Default.GetBytes(cmd);
                        client.Send(buffer);

                        buffer = new byte[4096];
                        int bytesRead = 0;

                        using (var ms = new MemoryStream())
                        {
                            while ((bytesRead = client.Receive(buffer)) > 0)
                            {
                                ms.Write(buffer, 0, bytesRead);
                            }

                            var text = Encoding.Default.GetString(ms.ToArray());
                            NavigationList = JsonConvert.DeserializeObject<List<FileItem>>(text);
                        }
                    }
                }
                catch (SocketException)
                {
                    // Exception while connecting
                    IsConnected = false;
                }
                catch (Exception ex)
                {
                    // General error
                    MessageBox.Show(ex.Message);
                }
                finally
                {
                    client.Close();
                }
            }
        }

        public void DownloadFile(string path, string name, long size)
        {
            if (IsCanceled) return;

            using (var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                try
                {
                    client.Connect(Address, 8080);

                    using (var ns = new NetworkStream(client))
                    {
                        byte[] buffer = Encoding.Default.GetBytes("SEND " + path);
                        client.Send(buffer);

                        buffer = new byte[4096];
                        int bytesRead = 0;
                        long bytesWrited = 0;

                        var musicDir = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);

                        var fileDir = Path.Combine(musicDir, Path.GetFileName(Path.GetDirectoryName(path)));
                        if (!Directory.Exists(fileDir)) Directory.CreateDirectory(fileDir);

                        var fs = File.OpenWrite(Path.Combine(fileDir, name));

                        while ((bytesRead = client.Receive(buffer)) > 0)
                        {
                            if (IsCanceled) break;

                            fs.Write(buffer, 0, bytesRead);

                            bytesWrited += bytesRead;

                            Application.Current.Dispatcher.Invoke(new Action(() =>
                            { Progress = (int)(100 * bytesWrited / (double)size); }), DispatcherPriority.Normal);
                        }

                        fs.Close();
                    }
                }
                catch (SocketException)
                {
                    // Exception while connecting
                    IsConnected = false;
                }
                catch (Exception ex)
                {
                    // General error
                    MessageBox.Show(ex.Message);
                }
                finally
                {
                    client.Close();
                }
            }
        }

        public void DownloadFiles(FileItem[] items)
        {
            CurrentFile = "";
            Progress = 0;
            TotalProgress = 0;
            IsCanceled = false;

            new Thread(() =>
            {
                Application.Current.Dispatcher.Invoke(new Action(() =>
                { IsDownloading = true; }), DispatcherPriority.Normal);

                int i = 0;

                foreach (var item in items)
                {
                    if (IsCanceled) break;

                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    { CurrentFile = item.Name; }), DispatcherPriority.Normal);

                    DownloadFile(item.Path, item.Name, item.Size);

                    i++;

                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    { TotalProgress = 100 * i / (double)items.Length; }), DispatcherPriority.Normal);
                }

                Application.Current.Dispatcher.Invoke(new Action(() =>
                { IsDownloading = false; }), DispatcherPriority.Normal);

            }).Start();
        }
    }
}
