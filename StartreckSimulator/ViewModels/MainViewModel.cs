using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using StartreckSimulator.Annotations;
using StartreckSimulator.Models;
using StartreckSimulator.Properties;
using StartreckSimulator.Views;

namespace StartreckSimulator.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        #region / / / / /  Private fields  / / / / /

        private string _url;
        private bool _isRunning;
        private ClassificationServer _server;
        private int _serverStatus = 200;
        private bool _showKeepAlive;
        private bool _isSuccess = true;

        #endregion

        #region / / / / /  Properties  / / / / /

        public string Url
        {
            get => _url;
            set
            {
                _url = value;
                OnPropertyChanged(nameof(Url));
            }
        }

        public ObservableCollection<ClassificationSelect> Classifications { get; set; }

        public ObservableCollection<ListViewItem> LogItems { get; set; } = new ObservableCollection<ListViewItem>();

        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                _isRunning = value;
                OnPropertyChanged(nameof(IsRunning));
            }
        }

        public int ServerStatus
        {
            get => _serverStatus;
            set
            {
                _serverStatus = value;
                if (_server != null)
                {
                    _server.StatusCode = value;
                }
                OnPropertyChanged(nameof(ServerStatus));
            }
        }

        public bool ShowKeepAlive
        {
            get => _showKeepAlive;
            set
            {
                _showKeepAlive = value;
                OnPropertyChanged(nameof(ShowKeepAlive));
            }
        }

        public bool IsSuccess
        {
            get => _isSuccess;
            set
            {
                _isSuccess = value;
                if (_server != null)
                {
                    _server.IsSuccess = value;
                }
                OnPropertyChanged(nameof(IsSuccess));
            }
        }

        #endregion

        #region / / / / /  Commands  / / / / /

        public ICommand StartStopCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand CloseCommand { get; set; }
        public ICommand LoadCommand { get; set; }
        public ICommand UpdateCommand { get; set; }

        #endregion

        #region / / / / /  Private methods  / / / / /

        private void AddLogItem(string header, object content = null)
        {
            Application.Current.Dispatcher?.Invoke(() =>
            {
                ListViewItem item = new ListViewItem
                {
                    Content = $"{DateTime.Now} - {header}",
                };
                item.MouseDoubleClick += (sender, args) =>
                {
                    if (content != null)
                    {
                        LogItemWindow itemWindow = new LogItemWindow(content);
                        itemWindow.Show();
                        itemWindow.Activate();
                    }
                    else
                    {
                        LogItemWindow itemWindow = new LogItemWindow(header);
                        itemWindow.Show();
                        itemWindow.Activate();
                    }
                };

                LogItems.Insert(0, item);
                // no more than max value
                if (LogItems.Count > Settings.Default.MaxLogItems)
                {
                    // remove last
                    LogItems.RemoveAt(LogItems.Count - 1);
                }
            });
        }

        private void Load()
        {
            Url = Settings.Default.ServerUrl;
            AddLogItem("App Started");
        }

        private void Close()
        {
            _server?.Stop();
            Settings.Default.ServerUrl = Url;
            Settings.Default.Save();
        }

        private void ClearLog()
        {
            LogItems.Clear();
        }

        private void ToggleStartStop()
        {
            if (_server != null && _server.IsOpen)
            {
                try
                {
                    _server.Stop();
                    IsRunning = false;
                    AddLogItem("Server Stopped");
                }
                catch (Exception ex)
                {
                    AddLogItem("Error Stopping Server", ex);
                }
            }
            else
            {
                try
                {
                    _server = new ClassificationServer(new Uri(Url, UriKind.Absolute));
                    _server.RequestReceived += Server_RequestReceived;
                    _server.ResponseSent += Server_ResponseSent;
                    _server.Error += Server_Error;
                    _server.AckSent += Server_AckSent;
                    _server.Start();
                    _server.StatusCode = ServerStatus;
                    UpdateClassification();
                    IsRunning = true;
                    AddLogItem($"Server Started on {_server.ServerUrl}");
                }
                catch (Exception ex)
                {
                    AddLogItem("Error Starting Server", ex);
                }
            }
        }

        private void Server_AckSent(object sender, Acknowledge e)
        {
            if (ShowKeepAlive)
            {
                AddLogItem("Acknowledge Message Sent", e.ToJson());
            }
        }

        private void Server_Error(object sender, Exception e)
        {
            AddLogItem("Error in WebSocket Server", e);
        }

        private void Server_ResponseSent(object sender, Response e)
        {
            AddLogItem("Response Sent", e.ToJson());
        }

        private void Server_RequestReceived(object sender, Request e)
        {
            if (e.Command != "KeepAlive" || ShowKeepAlive)
            {
                AddLogItem($"Request ({e.Command}) Received", e.ToJson());
            }
        }

        private void UpdateClassification()
        {
            _server?.UpdateClassifications(Classifications.Where(x => x.IsSelected).Select(x => x.Type));
        }

        #endregion

        #region / / / / /  Public methods  / / / / /

        public MainViewModel()
        {
            StartStopCommand = new Command(ToggleStartStop);
            ClearCommand = new Command(ClearLog);
            CloseCommand = new Command(Close);
            LoadCommand = new Command(Load);
            UpdateCommand = new Command(UpdateClassification);

            var array = Enum.GetValues(typeof(ClassificationTypes));
            var classifications = new ClassificationTypes[array.Length];
            array.CopyTo(classifications, 0);

            Classifications = new ObservableCollection<ClassificationSelect>(
                classifications.Select(x => new ClassificationSelect {IsSelected = false, Type = x}));
        }

        #endregion

        #region / / / / /  INotifyPropertyChanged  / / / / /
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        } 
        #endregion
    }
}