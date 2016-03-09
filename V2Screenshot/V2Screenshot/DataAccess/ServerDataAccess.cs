using V2Screenshot.Error;
using V2Screenshot.Model;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using V2Screenshot.Extension;

namespace V2Screenshot.DataAccess
{
    class ServerDataAccess : DataAccessBase
    {
        private Server server;
        private string lastStatus = "";
        private DateTime statusTime;

        public string LastStatus
        {
            get
            {
                return lastStatus;
            }

            set
            {
                lastStatus = value;
                statusTime = DateTime.Now;
            }
        }


        public ServerDataAccess(Server server)
        {
            this.server = server;
        }

        public async Task<string> GetStatusAsync()
        {
            if(LastStatus != null && LastStatus != String.Empty && statusTime != null)
            {
                if(statusTime > DateTime.Now - TimeSpan.FromSeconds(1))
                {
                    return LastStatus;
                }
            }


            using (UdpClient udpClient = new UdpClient())
            {
                try
                {

                    udpClient.Connect(server.Address, server.Port);


                    Byte[] tmpBytes = Encoding.ASCII.GetBytes("getstatus");
                    byte[] sendBytes = new byte[tmpBytes.Length + 4];

                    sendBytes[0] = byte.Parse("255");
                    sendBytes[1] = byte.Parse("255");
                    sendBytes[2] = byte.Parse("255");
                    sendBytes[3] = byte.Parse("255");

                    int j = 4;

                    for (int i = 0; i < tmpBytes.Length; i++)
                    {
                        sendBytes[j++] = tmpBytes[i];
                    }


                    await udpClient.SendAsync(sendBytes, sendBytes.Length);


                    udpClient.SetTimeout(TimeSpan.FromSeconds(5));

                    UdpReceiveResult receive = await udpClient.ReceiveAsync();

                    string status = Encoding.ASCII.GetString(receive.Buffer);

                    LastStatus = status;

                    return status;
                }
                catch (SocketException)
                {
                    throw new ApiException("Server unreachable");
                }
                catch (ObjectDisposedException)
                {
                    throw new ApiException("Server connection timed out");
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                  
               
            }
        }

        public async Task<List<Player>> GetPlayersAsync()
        {
  
            try
            {

                string status = await GetStatusAsync();

                String[] lines = status.Split('\n');

                Regex rx = new Regex("(?<score>\\d+) (?<ping>\\d+) \"(?<name>.*)\"",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

                List<Player> players = new List<Player>();

                for (int i = 2; i < lines.Length - 1; ++i)
                {

                    GroupCollection info = rx.Match(lines[i]).Groups;

                    string name = info["name"].Value;
                    int score = Convert.ToInt32(info["score"].Value);
                    int ping = Convert.ToInt32(info["ping"].Value);

                    Player p = await V2DataAccess.GetPlayer(name, false);
                    p.Score = score;
                    p.Ping = ping;
                    players.Add(p);
                }
                server.Clients = lines.Length - 3;
                return players;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<string> SendRconCmd(string cmd)
        {
            using (UdpClient udpClient = new UdpClient())
            {
                string response = null;
                for (int tries = 0; tries < 3; tries++)
                {
                    try
                    {

                        udpClient.Connect(server.Address, server.Port);


                        Byte[] tmpBytes = Encoding.ASCII.GetBytes(String.Format("rcon {0} {1}", server.RconPassword, cmd));
                        byte[] sendBytes = new byte[tmpBytes.Length + 4];

                        sendBytes[0] = byte.Parse("255");
                        sendBytes[1] = byte.Parse("255");
                        sendBytes[2] = byte.Parse("255");
                        sendBytes[3] = byte.Parse("255");

                        int j = 4;

                        for (int i = 0; i < tmpBytes.Length; i++)
                        {
                            sendBytes[j++] = tmpBytes[i];
                        }


                        await udpClient.SendAsync(sendBytes, sendBytes.Length);


                        udpClient.SetTimeout(TimeSpan.FromSeconds(5));

                        UdpReceiveResult receive = await udpClient.ReceiveAsync();

                        response = Encoding.ASCII.GetString(receive.Buffer);

                    }
                    catch (SocketException)
                    {
                        if (tries < 2)
                            continue;
                    }
                    catch (ObjectDisposedException)
                    {
                        throw new ApiException("Server connection timed out");
                    }
                    catch (Exception ex)
                    {
                        Console.Write(ex);
                        throw ex;
                    }
                }
                if(response != null)
                {
                    return response;
                }
                else
                {
                    throw new ApiException("Server unreachable");
                }
            }
        }
        

    }

    
}