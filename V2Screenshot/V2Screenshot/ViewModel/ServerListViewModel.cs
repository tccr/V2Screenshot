using V2Screenshot.DataAccess;
using V2Screenshot.Error;
using V2Screenshot.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Timers;
using System.Linq;
using System.ComponentModel;
using System.Windows.Data;

namespace V2Screenshot.ViewModel
{
    class ServerListViewModel : WorkspaceViewModel, IDisposable
    {
        
        private Timer RefreshTimer = new Timer(10000);
        private bool autoRefresh = true;
        private CollectionViewSource serverCollection;
        private string filterText = "";
        private bool filterFavorites = false;
        

        #region properties
        public ObservableCollection<ServerViewModel> Servers 
        { 
            get 
            {
                return SelectedGame.Servers;
            }

            private set
            {
                if(value != SelectedGame.Servers)
                {
                    SelectedGame.Servers = value;
                    NotifyPropertyChanged("Servers");
                }
            }
        }

        
        public List<Game> Games { get; private set; }

        
        public override string Title
        {
            get
            {
                return "Server Browser";
            }
            
        }

        public bool AutoRefresh
        {
            get
            {
                return autoRefresh;
            }
            set
            {
                if (autoRefresh != value)
                {
                    autoRefresh = value;
                    NotifyPropertyChanged("AutoRefresh");
                }
            }
        }

        public int ServerCount
        {
            get
            {
                return Servers.Where(x => x.Show).Count();
            }
        }

        public int TotalServerCount
        {
            get
            {
                return Servers.Where(x => x.Show).Count();
            }
        }

        public int PlayerCount
        {
            get
            {
                return Servers.Where(x => x.Show).Sum(x => x.Clients);
            }
        }

        public Game SelectedGame
        {
            get
            {
                return V2DataAccess.Game;
            }
            set
            {
                V2DataAccess.Game = value;
                NotifyPropertyChanged("SelectedGame");
            }
        }

        public string FilterText 
        {
            get
            {
                return filterText;
            }

            set
            {
                if(filterText != value)
                {
                    filterText = value;
                    NotifyPropertyChanged("FilterText");
                }
            }
        }

        public bool FilterFavorites
        {
            get
            {
                return filterFavorites;
            }

            set
            {
                if (filterFavorites != value)
                {
                    filterFavorites = value;
                    NotifyPropertyChanged("FilterFavorites");
                }
            }
        }

        public ICollectionView SourceCollection
        {
            get
            {
                return serverCollection.View;
            }
        }


        #endregion // properties


        #region commands

        public Command RefreshCommand { get; private set; }
        #endregion commands


        #region constructor
        public ServerListViewModel():base(false)
        {
            

            Games = new List<Game>();
            Games.Add(new Game(61586, "MW2"));
            //Games.Add(new Game(2117, "BO"));
            SelectedGame = Games[0];

            foreach(Game g in Games)
            {
                g.Servers = new ObservableCollection<ServerViewModel>();
                g.Servers.CollectionChanged += Servers_CollectionChanged;
            }
            

            serverCollection = new CollectionViewSource();
            serverCollection.Source = SelectedGame.Servers;
            serverCollection.Filter += serverCollection_Filter;
            serverCollection.SortDescriptions.Add(new SortDescription("Clients", ListSortDirection.Descending));

            

            RefreshCommand = new Command(CmdRefresh, CanRefresh);
            RefreshTimer.Elapsed += RefreshTimer_Elapsed;
            RefreshTimer.AutoReset = false;

            this.PropertyChanged += ServerListViewModel_PropertyChanged;

            UpdateServers();
        }

        

        
        
        #endregion //constructor


        #region command methods

        private bool CanRefresh()
        {
            return !IsLoading;
        }

        private void CmdRefresh()
        {
            UpdateServers();
        }
        
        #endregion //command methods


        #region methods


        private async void UpdateServers()
        {
              
            IsLoading = true;
           
            try
            {
                await V2DataAccess.UpdateCollection(Servers, V2DataAccess.GetServersAsync, x => x.Server, x => Add(x), false);
                foreach(ServerViewModel s in Servers)
                {
                    try
                    {
                        s.UpdateServerInfo();
                    }
                    catch(ApiException ex)
                    {
                        SetError(ex, UpdateServers);
                        
                    }
                    catch (Exception ex)
                    {
                        SetError(ex, UpdateServers);
                    }
                    
                }
                
                if(AutoRefresh)
                {
                    RefreshTimer.Start();
                }
                
            }
            catch(ExceptionBase ex)
            {
                SetError(ex, UpdateServers);
                
            }
            catch (Exception)
            {
                SetError(new Exception("Unknown error"), UpdateServers);
                
            }
            finally
            {
                IsLoading = false;
            }
            

        }
        private void Add(Server s)
        {
            if(!Servers.Any(x => x.Server.Equals(s)))
            {
                ServerViewModel vm = V2DataAccess.GetServerVM(s);
                Servers.Add(vm);
                vm.PropertyChanged += ServerViewModel_PropertyChanged;
            }
        }

        public void Dispose()
        {
            RefreshTimer.Dispose();
            RefreshTimer = null;
        }

        private void AutoRefreshChanged()
        {
            RefreshTimer.Enabled = AutoRefresh;
        }

        private void GameChanged()
        {
            UpdateServers();
            NotifyPropertyChanged("Servers");

            
        }

        #endregion //methods


        #region event handler methods

        void RefreshTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            
            App.Current.Dispatcher.Invoke((Action)delegate
            {
                UpdateServers();
            });
            
        }

        void ServerListViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch(e.PropertyName)
            {
                case "IsLoading":
                    RefreshCommand.NotifyCanExecuteChanged();
                    break;
                case "AutoRefresh":
                    AutoRefreshChanged();
                    break;
                case "SelectedGame":

                    GameChanged();
                    break;
                case "Servers":
                    serverCollection.Source = SelectedGame.Servers;
                    serverCollection.Filter += serverCollection_Filter;
                    NotifyPropertyChanged("SourceCollection");
                    NotifyPropertyChanged("ServerCount");
                    break;
                case "ServerCount":
                    NotifyPropertyChanged("TotalServerCount");
                    NotifyPropertyChanged("PlayerCount");
                    break;
                case "FilterText":
                case "FilterFavorites":
                    NotifyPropertyChanged("Servers");
                    break;
            }
        }

        void ServerViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            ServerViewModel vm = sender as ServerViewModel;
            if (vm != null)
            {
                switch (e.PropertyName)
                {
                    case "ClientCount":
                        Servers.Remove(vm);
                        Servers.Add(vm);
                        NotifyPropertyChanged("PlayerCount");
                        break;

                    case "Error":
                        if (vm.Hostname == "unknown" || (!vm.IsOpen && vm.Error != null))
                            vm.Show = false;
                        else if (vm.Error == null)
                            vm.Show = true;
                        break;

                    case "Show":
                    case "IsFavorite":
                        NotifyPropertyChanged("Servers");
                        break;

                }
            }
        }

        void Servers_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            NotifyPropertyChanged("ServerCount");
        }

        void serverCollection_Filter(object sender, FilterEventArgs e)
        {
            ServerViewModel vm = e.Item as ServerViewModel;
            
            e.Accepted = vm.Show;

            if(e.Accepted && FilterText != "")
            {
                e.Accepted = vm.Hostname.Contains(FilterText) || vm.Players.Any(x => x.CleanName.Contains(FilterText));
            }

            if(e.Accepted && FilterFavorites)
            {
                e.Accepted = vm.IsFavorite;
            }

        }

        #endregion //event handler methods


    }
}
