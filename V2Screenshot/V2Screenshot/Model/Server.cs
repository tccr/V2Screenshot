﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace V2Screenshot.Model
{
    class Server : ModelBase, IEquatable<Server>
    {

        private string hostname;
        private IPAddress address;
        private int port;
        private int npid;
        private int clients;
        private int maxClients;
        private string map;
        private string gameType;
        private Game game;
        private string rconPass;


        #region properties

        public string Hostname
        {
            get
            {
                return hostname;
            }
            set
            {
                if (hostname != value)
                {
                    hostname = value;
                    NotifyPropertyChanged("Hostname");
                }
            }
        }

        public IPAddress Address
        {
            get
            {
                return address;
            }
            set
            {
                if (address != value)
                {
                    address = value;
                    NotifyPropertyChanged("Address");
                }
            }
        }

        public int Port
        {
            get
            {
                return port;
            }
            set
            {
                if (port != value)
                {
                    port = value;
                    NotifyPropertyChanged("Port");
                }
            }
        }

        public int Npid
        {
            get
            {
                return npid;
            }
            set
            {
                if (npid != value)
                {
                    npid = value;
                    NotifyPropertyChanged("Npid");
                }
            }
        }

        public int Clients
        {
            get
            {
                return clients;
            }
            set
            {
                if (clients != value)
                {
                    clients = value;
                    NotifyPropertyChanged("Clients");
                }
            }
        }

        public int MaxClients
        {
            get
            {
                return maxClients;
            }
            set
            {
                if (maxClients != value)
                {
                    maxClients = value;
                    NotifyPropertyChanged("MaxClients");
                }
            }
        }

        public string Map
        {
            get
            {
                return map;
            }
            set
            {
                if (map != value)
                {
                    map = value;
                    NotifyPropertyChanged("Map");
                }
            }
        }

        public string GameType
        {
            get
            {
                return gameType;
            }
            set
            {
                if (gameType != value)
                {
                    gameType = value;
                    NotifyPropertyChanged("GameType");
                }
            }
        }

        public Game Game
        {
            get
            {
                return game;
            }
            set
            {
                if (game != value)
                {
                    game = value;
                    NotifyPropertyChanged("Game");
                }
            }
        }

        public string RconPassword
        {
            get
            {
                return rconPass;
            }
            set
            {
                if (rconPass != value)
                {
                    rconPass = value;
                    NotifyPropertyChanged("RconPassword");
                }
            }
        }

        #endregion //properties


        #region constructor

        public Server(string hostname, IPAddress address, int port, int npid, int clients, int maxclients, string map, string gametype)
        {
            Hostname = hostname;
            Address = address;
            Port = port;
            Npid = npid;
            Clients = clients;
            MaxClients = maxclients;
            Map = map;
            GameType = gametype;
        }

        #endregion // constructor


        #region methods

        public override bool Update(ModelBase s)
        {
            if(s is Server)
            {
                return UpdateProperty("MaxClients", ((Server)s).MaxClients);
            }
            else
            {
                return false;
            }
            
        }

        public override string ToString()
        {
            return String.Format("[{0}]{1}", Npid, Hostname);
        }
        #endregion // methods


        #region interfaces

        public bool Equals(Server s)
        {
            //return (this.Address.ToString().Equals(s.Address.ToString()) && this.Port.Equals(s.Port));
            return (this.Npid.Equals(s.Npid) || (Address.Equals(s.Address) && Port.Equals(s.Port)));
        }

        #endregion //interfaces
    }
}
