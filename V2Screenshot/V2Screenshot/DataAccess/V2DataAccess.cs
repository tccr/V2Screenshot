using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using V2Screenshot.Error;
using V2Screenshot.Model;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Reflection;
using V2Screenshot.ViewModel;
using System.Net.Sockets;
using System.Diagnostics;
using com.LandonKey.SocksWebProxy;
using com.LandonKey.SocksWebProxy.Proxy;


namespace V2Screenshot.DataAccess
{
    class V2DataAccess : DataAccessBase
    {
        private const string API_BASE = "http://fjql7u2zyeb4vwdk.onion/api/";
        private const double API_DELAY = 1;
        private const int API_TRIES = 3;
        protected static string USER_AGENT = "V2 Screenshot Tool/" 
            + Assembly.GetExecutingAssembly().GetName().Version.ToString() 
            + "(by tccr(80))";

        private const string TOR_ARGS = "--SOCKSPort 9050 --AutomapHostsOnResolve 1 --AllowSingleHopCircuits 1";
        private static Dictionary<string, Player> PlayerCache;
        private static Dictionary<Player, PlayerViewModel> PlayerVMCache;
        private static Dictionary<Server, ServerViewModel> ServerVMCache;
        
        private static DateTime LastRequest { get; set; }

        public static Game Game { get; set; }

        private static Process TorProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                RedirectStandardOutput = true,
                FileName = AppDomain.CurrentDomain.BaseDirectory + "tor/tor.exe",
                Arguments = TOR_ARGS,
                
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        private static bool IsTorReady = false;

        

        static V2DataAccess()
        {
            LastRequest = new DateTime();
            PlayerCache = new Dictionary<string, Player>();
            PlayerVMCache = new Dictionary<Player, PlayerViewModel>();
            ServerVMCache = new Dictionary<Server, ServerViewModel>();

            TorProcess.Start();
            
        }

        private async static Task<bool> IsTorStarted()
        {
            if (IsTorReady)
                return true;
            
            string line;
            try
            {
                while ((line = await TorProcess.StandardOutput.ReadLineAsync()) != String.Empty)
                {
                    if (line.Contains("Done"))
                    {
                        IsTorReady = true;
                        return true;
                    }
                }
                return false;
            }
            catch(Exception)
            {
                return false;
            }
        }

        private static int GetFreePort()
        {
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            int port = ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();

            return port;
        }

        protected static async Task<dynamic> ApiCallAsync(string url)
        {
           
            if(!url.EndsWith("/"))
            {
                url += "/";
            }

            string res = String.Empty;

            for(int i = 1; i <= API_TRIES; ++i)
            {

                TimeSpan diff;

                while((diff = DateTime.Now - LastRequest) < TimeSpan.FromSeconds(API_DELAY))
                {

                    TimeSpan delay = (TimeSpan.FromSeconds(API_DELAY) - diff);
                    if(delay < TimeSpan.FromMilliseconds(1))
                    {
                        delay = TimeSpan.FromMilliseconds(100);
                    }
                    await Task.Delay(delay);

                }
                LastRequest = DateTime.Now;

                while (!TorProcess.HasExited && !await IsTorStarted())
                {
                    await Task.Delay(100);
                }
                
                try
                {
                    using (var client = new WebClient())
                    {
                        var conf = new ProxyConfig(IPAddress.Parse("127.0.0.1"), GetFreePort(), IPAddress.Parse("127.0.0.1"), 9050, com.LandonKey.SocksWebProxy.Proxy.ProxyConfig.SocksVersion.Five);
                        client.Proxy = new SocksWebProxy(conf);
                        
                        //set headers
                        client.Headers.Set("User-Agent", USER_AGENT);
                        
                        res = await client.DownloadStringTaskAsync(new Uri(url));
                        
                        if (res != String.Empty)
                            break;

                    }
                }
                catch(WebException ex)
                {
                    if (ex.Status == WebExceptionStatus.ProtocolError)
                    {
                        if (i < API_TRIES)
                            continue;
                        else
                            throw new InvalidResponseException();
                    }
                        
                    throw new ApiException("Server unreachable");
                }
                catch(Exception ex)
                {
                    System.Windows.MessageBox.Show(ex.Message);
                    throw ex;
                }

                finally
                {
                    LastRequest = DateTime.Now;
                }
            }
            try
            {
                dynamic json = JsonConvert.DeserializeObject(res);

                return json;
            }
            catch(Exception)
            {
                throw new InvalidResponseException();
            }

        }


        public static async Task GetIdAsync(Player player)
        {
            try
            {
                Player p = await GetPlayer(player.Name);
                player.Id = p.Id;
                player.Name = p.Name;
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

        public static async Task GetPresenceDataAsync(Player p)
        {
            dynamic json = await ApiCallAsync(API_BASE + "presenceData/" + p.Id);

            if(json.status == 200)
            {
                dynamic result = JObject.Parse((string)json.result);
                string hostname = result.hostname;
                string game = result.game;
                //string country = json.connection_data.Country;
                
                p.Hostname = hostname;
                p.Game = game;
                p.Country = "Unknown";

            }
            else if(json.status == 204 && json.result == "Offline")
            {
                throw new UserOfflineException(p);
            }

        }

        public static async Task<List<Player>> FindPlayers(string name)
        {
            List<Player> players = new List<Player>();
            try
            {
                dynamic json = await ApiCallAsync(API_BASE + "username/" + name);
                if(json.result != null)
                {
                    foreach(dynamic player in json.result)
                    {
                        int id = player.user_id;
                        string username = player.username;
                        Player p;

                        //get player object from cache
                        if(!PlayerCache.TryGetValue(username, out p))
                        {
                            p = new Player(id, username);
                            PlayerCache.Add(username, p);
                        }
                        p.Id = id;
                        p.Name = username;

                        players.Add(p);
                    }
                }

                return players;
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

        public static async Task<Player> GetPlayer(string name)
        {
            return await GetPlayer(name, true);
        }

        public static async Task<Player> GetPlayer(string name, bool online)
        {
            Player player = new Player(0, name);
            //check cache
            if(PlayerCache.ContainsKey(name))
            {
                player = PlayerCache[name];
                if (!online || player.Id != 0)
                    return player;
            }

            if(!online)
            {
                PlayerCache.Add(name, player);
                return player;
            }

            try
            {
                List<Player> players = await FindPlayers(name);

                if(players.Count == 0)
                {
                    throw new UserNotFoundException();
                }
                else if(players.Count == 1)
                {
                    return players[0];
                }

                //check for exact match
                foreach(Player p in players)
                {
                    if(p.Name == name)
                        return p;

                }

                //find player beginning with name
                foreach(Player p in players)
                {
                    if(p.Name.StartsWith(name))
                        return p;
                }

                //return first in the list
                return players[0];
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

        public static async Task<BitmapImage> GetScreenshotAsync(Player player)
        {
            bool done = false;
            for(int tries = 0; tries < 20 && !done; ++tries)
            {
                try
                {
                    dynamic json = await ApiCallAsync(API_BASE + "screenshot/" + player.Id);

                    if(json.status == 200 && json.result != "Waiting for answer.")
                    {
                        string img = json.result;
                        Byte[] bitmapData = new Byte[img.Length];
                        bitmapData = Convert.FromBase64String(img);
                        System.IO.MemoryStream streamBitmap = new System.IO.MemoryStream(bitmapData);
                        BitmapImage bitImage = new BitmapImage();
                        bitImage.BeginInit();
                        bitImage.StreamSource = streamBitmap;
                        bitImage.EndInit();

                        return bitImage;

                    }
                    else if(json.result == "Offline")
                    {
                        throw new UserOfflineException(player);
                    }
                    else if((json.status == 200 && json.result == "Waiting for answer.") || (json.status == 204 && json.result == "Request sent."))
                    {

                        await Task.Delay(2000);
                    }
                    else
                    {
                        break;
                    }
                }
                catch(Exception ex)
                {
                    Console.Write(ex.StackTrace);
                    throw ex;
                }
            }


            //unable to get screenshot
            throw new ScreenshotException("Unable to get Screenshot");


        }

        public static async Task<List<Server>> GetServersAsync()
        {
            try
            {
                dynamic json = await ApiCallAsync(API_BASE + String.Format("findSessions/{0}/1/", Game.NetCode));
                List<Server> servers = new List<Server>();
                foreach(dynamic server in json)
                {
                    //string hostname = server.Info.hostname;
                    IPAddress address = IPAddress.Parse(server.session.address.ToString());

                    int port = server.session.port;
                    //int clients = server.Info.clients;
                    //int maxclients = server.Info.sv_maxclients;
                    //string map = server.Info.mapname;
                    //string gametype = server.Info.gametype;

                    int npid = server.session.npid;

                    int clients = 0;
                    int maxclients = server.session.maxplayers;
                    string map = "unknown";
                    string gametype = "unknown";
                    string hostname = "unknown";

                    Server s = new Server(hostname, address, port, npid, clients, maxclients, map, gametype);
                    s.Game = Game;

                    servers.Add(s);
                }

                return servers;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
                throw ex;
            }
        }

        public static PlayerViewModel GetPlayerVM(Player p)
        {
            PlayerViewModel vm = null;
            if(!PlayerVMCache.TryGetValue(p, out vm))
            {
                vm = new PlayerViewModel(p);
                PlayerVMCache.Add(p, vm);
            }
            
            return vm;
        }

        public static ServerViewModel GetServerVM(Server s)
        {
            ServerViewModel vm = null;
            if (!ServerVMCache.TryGetValue(s, out vm))
            {
                vm = new ServerViewModel(s);
                ServerVMCache.Add(s, vm);
            }

            return vm;
        }

    }


    
}
