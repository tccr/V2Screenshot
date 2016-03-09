﻿using V2Screenshot.DataAccess;
using V2Screenshot.Error;
using V2Screenshot.Helper;
using V2Screenshot.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Timers;
using System.Linq;
using System.Windows.Media;
using System.Collections.Specialized;
using System.Windows.Media.Imaging;
using System.Text.RegularExpressions;

namespace V2Screenshot.ViewModel
{
    class ServerViewModel : WorkspaceViewModel, IDisposable
    {
        private bool show = true;

        #region Properties

        public Server Server { get; private set; }

        public ServerDataAccess ServerDataAccess { get; private set; }

        private PlayerListViewModel PlayerListVM { get; set; }

        public ObservableCollection<WorkspaceViewModel> Tabs { get; private set; }

        public int SelectedTabIndex { get; set; }

        public override string Title
        {
            get
            {
                return Hostname + String.Format(" ({0})", Clients);
            }

        }

        public string Hostname
        {
            get
            {
                return UIHelper.RemoveColor(Server.Hostname);
            }
            set
            {
                if (value != Server.Hostname)
                {
                    Server.Hostname = value;
                    NotifyPropertyChanged("Hostname");
                }
            }
        }

        public IPAddress Address
        {
            get
            {
                return Server.Address;
            }
            set
            {
                if (value != Server.Address)
                {
                    Server.Address = value;
                    NotifyPropertyChanged("Address");
                }
            }
        }

        public int Port
        {
            get
            {
                return Server.Port;
            }
            set
            {
                if (value != Server.Port)
                {
                    Server.Port = value;
                    NotifyPropertyChanged("Port");
                }
            }
        }

        public int Clients
        {
            get
            {
                return Server.Clients;
            }
        }
        public string ClientCount
        {
            get
            {
                return String.Format("{0} / {1}", Server.Clients.ToString(), Server.MaxClients.ToString());
            }
        }

        public string MapName
        {
            get
            {
                return UIHelper.GetMapName(Server.Map);
            }

        }

        public string GameType
        {
            get
            {

                return UIHelper.GetGameTypeName(Server.GameType);
            }
        }

        public bool Show
        {
            get
            {
                return show;
            }
            set
            {
                if(show != value)
                {
                    show = value;
                    NotifyPropertyChanged("Show");
                }
            }
        }

        public Brush StatusBrush
        {
            get
            {
                if (Error != null || Tabs.Any(x => x.Error != null))
                    return Brushes.Red;

                else if (Players.Count == 0)
                    return null;

                else if (Players.Any(x => x.IsLoading))
                    return Brushes.Yellow;

                else if (Players.All(x => x.ScreenshotTaken))
                    return Brushes.Green;

                return null;
            }
        }

        public bool IsFavorite
        {
            get
            {
                return Properties.Settings.Default.Favorites.Contains(String.Format("{0}:{1}", Address, Port));
            }

            set
            {
                if(Properties.Settings.Default.Favorites.Contains(String.Format("{0}:{1}", Address, Port)) != value)
                {
                    if (value)
                        Properties.Settings.Default.Favorites.Add(String.Format("{0}:{1}", Address, Port));
                    else
                        Properties.Settings.Default.Favorites.Remove(String.Format("{0}:{1}", Address, Port));

                    Properties.Settings.Default.Save();
                    NotifyPropertyChanged("IsFavorite");
                }
            }
        }


        public ObservableCollection<PlayerViewModel> Players { get; private set; }


        #endregion //properties


        #region Commands
        public Command OpenCommand { get; private set; }
        
        #endregion //Commands


        #region constructor
        public ServerViewModel(Server server)
        {
            Server = server;



            Server.PropertyChanged += Server_PropertyChanged;

            ServerDataAccess = new ServerDataAccess(Server);
            PlayerListVM = new PlayerListViewModel(this);

            Tabs = new ObservableCollection<WorkspaceViewModel>();
            Tabs.CollectionChanged += Tabs_CollectionChanged;
            Tabs.Add(PlayerListVM);

            OpenCommand = new Command(CmdOpen, CanOpen);
            

            Players = new ObservableCollection<PlayerViewModel>();
            Players.CollectionChanged += Players_CollectionChanged;

            this.PropertyChanged += ServerViewModel_PropertyChanged;

            
            
        }



        #endregion //constructor


        #region command methods
        private bool CanOpen()
        {
            return !(MainWindowViewModel.Workspaces.Contains(this));
        }

        private void CmdOpen()
        {
            MainWindowViewModel.AddWorkspace(this);
            this.OnOpen();
            this.RequestClose += ServerViewModel_RequestClose;
            OpenCommand.NotifyCanExecuteChanged();
        }

        
       
        #endregion //command methods


        #region methods

        private string ParseVariable(string status, string var)
        {
            return Regex.Match(status, var + @"\\(?<" + var + @">[^\\]*)", RegexOptions.IgnoreCase).Groups[var].Value;
        }

        public async void UpdateServerInfo()
        {
            try
            {
                
                await ServerDataAccess.UpdateCollection(Players, ServerDataAccess.GetPlayersAsync, x => x.Player, x => Players.Add(V2DataAccess.GetPlayerVM(x)), true);

                string status = await ServerDataAccess.GetStatusAsync();

                Server.Hostname = ParseVariable(status, "sv_hostname");
                Server.Map = ParseVariable(status, "mapname");
                Server.GameType = ParseVariable(status, "g_gametype");
                Server.Clients = status.Split('\n').Length - 3;

                RemoveError();
            }
            catch(ApiException ex)
            {
                SetError(ex, UpdateServerInfo);
            }
            
        }

        

        public void Dispose()
        {
            PlayerListVM.Dispose();
        }


        #endregion //methods


        #region event handler methods


        private void ServerViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch(e.PropertyName)
            {
                case "Error":
                    NotifyPropertyChanged("StatusBrush");
                    break;
            }
        }


        void ServerViewModel_RequestClose(object sender, EventArgs e)
        {
            OpenCommand.NotifyCanExecuteChanged();
            this.RequestClose -= ServerViewModel_RequestClose;
            
            foreach (PlayerViewModel pvm in Players)
            {
                pvm.CloseCommand.Execute(null);
            }
            
        }

        void Server_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            string property = null;
            switch(e.PropertyName)
            {
                case "Hostname":
                    property = "Hostname";
                    break;
                case "Address":
                    property = "Address";
                    break;
                case "Port":
                    property = "Port";
                    break;
                case "Clients":
                case "MaxClients":
                    property = "ClientCount";
                    break;
                case "Map":
                    property = "MapName";
                    break;
                case "GameType":
                    property = "GameType";
                    break;
                case "RconPassword":
                    property = "RconPassword";
                    break;
            }
            if (property != null)
            {
                NotifyPropertyChanged(property);
                if (property == "Hostname" || property == "ClientCount")
                    NotifyPropertyChanged("Title");
            }
                
            
        }

        void Players_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            NotifyPropertyChanged("StatusBrush");

            if (e.OldItems != null)
            {
                foreach (PlayerViewModel pvm in e.OldItems)
                {
                    pvm.PropertyChanged -= PlayerViewModel_PropertyChanged;
                }
            }

            if (e.NewItems != null)
            {
                foreach (PlayerViewModel pvm in e.NewItems)
                {
                    pvm.PropertyChanged += PlayerViewModel_PropertyChanged;
                }
            }
        }

        private void PlayerViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch(e.PropertyName)
            {
                case "Screenshot":
                case "IsLoading":
                    NotifyPropertyChanged("StatusBrush");
                    break;
            }
        }

        private void Tabs_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            NotifyPropertyChanged("StatusBrush");

            if (e.OldItems != null)
            {
                foreach (WorkspaceViewModel wvm in e.OldItems)
                {
                    wvm.PropertyChanged -= WorkspaceViewModel_PropertyChanged;
                }
            }

            if (e.NewItems != null)
            {
                foreach (WorkspaceViewModel wvm in e.NewItems)
                {
                    wvm.PropertyChanged += WorkspaceViewModel_PropertyChanged;
                }
            }
        }

        private void WorkspaceViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "Error":
                    NotifyPropertyChanged("StatusBrush");
                    break;
            }
        }

        
        #endregion //event handler methods




    }
}
