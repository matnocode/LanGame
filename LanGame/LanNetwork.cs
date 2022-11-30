using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading;

namespace ConsoleEngine
{
    public class LanNetwork
    {
        StreamWriter log = new StreamWriter("log1.txt");
        public delegate void GameListHandler();
        public event GameListHandler OnGameListChange;
        public event GameListHandler OnDataChange;
        public static NetworkInterface networkInterface;
        public static IPAddress networkDHCPAddress;


        public LanNetwork()
        {
            listOfGames = new List<Info>();
            ParameterizedThreadStart listenThreadStart = new ParameterizedThreadStart(Listen);

            Thread conListenThread = new Thread(listenThreadStart);
            Thread accListenThread = new Thread(listenThreadStart);
            Thread dataListenThread = new Thread(listenThreadStart);
            Thread ackListenThread = new Thread(listenThreadStart);
            conListenThread.Start(SchemeTypes.conrequest);
            accListenThread.Start(SchemeTypes.conaccept);
            dataListenThread.Start(SchemeTypes.condata);
            ackListenThread.Start(SchemeTypes.conack);
        }
        public static readonly Dictionary<SchemeTypes, int> schemePorts = new Dictionary<SchemeTypes, int>()
        {
            {SchemeTypes.conack, 3013 },
            {SchemeTypes.conrequest, 3012 },
            {SchemeTypes.conaccept, 3011 },
            {SchemeTypes.condata, 3010 }
        };
        public enum SchemeTypes
        {
            conrequest, conaccept, condata, conack
        }
        public struct Info
        {
            public static Info Empty()
            {
                return new Info() { empty = true };
            }
            public Info(string uname, IPAddress targetIP, IPAddress hostIP, int generated, int gameState, int sc = 0, string que = "", bool rev = false)
            {
                this.username = uname;
                this.targetIP = targetIP;
                this.host = hostIP;
                this.score = sc;
                this.query = que;
                this.empty = false;
                this.reveal = rev;
                this.generatedNumber = generated;
                this.currentGameState = gameState;

            }
            public bool isEmpty()
            {
                return empty;
            }
            public string username;
            public IPAddress targetIP;
            public int score;
            public string query;
            public int generatedNumber;
            public bool reveal;
            public IPAddress host;
            bool empty;
            public int currentGameState;
        }

        public List<Info> listOfGames;
        public Dictionary<IPAddress, Info> ReceivedData = new Dictionary<IPAddress, Info>();

        //has connection in a local network //ping to default gateway

        //fix
        static string GetValueFromQuery(string query, string dataName)
        {
            StreamWriter log = EngineControl.lanNetwork.log;
            log.WriteLine();
            log.WriteLine("----------------------Getting-Data-From-Query--------------------------");
            log.WriteLine("Query: " + query);
            log.WriteLine("Searcing for: " + dataName);
            if (query.Contains(dataName))
            {
                string dataQuery = query.Substring(query.IndexOf(dataName));
                if (dataQuery.Substring(dataQuery.IndexOf(dataName) + 1 + dataName.Length).Contains("&"))
                {
                    //+1 because of =
                    log.WriteLine("Got data: " + dataQuery.Substring(dataQuery.IndexOf(dataName) + 1 + dataName.Length, dataQuery.IndexOf("&", dataQuery.IndexOf(dataName) ) - dataName.Length-1));
                    log.Flush();
                    return dataQuery.Substring(dataQuery.IndexOf(dataName) + 1 + dataName.Length, dataQuery.IndexOf("&", dataQuery.IndexOf(dataName)) - dataName.Length - 1);
                }
                else
                {
                    log.WriteLine("Got data: " + dataQuery.Substring(dataQuery.IndexOf(dataName) + 1 + dataName.Length));
                    log.Flush();
                    return dataQuery.Substring(dataQuery.IndexOf(dataName) + 1 + dataName.Length);
                }
               
            }
            return null;
        }
        public static Info GetDataFromQuery(string uriString)
        {
            Uri uri = new Uri(uriString);
            string query = uri.Query;

            Info info = new Info();

            info.targetIP = IPAddress.Parse(uri.Host);
            info.query = uri.Query;

            info.username = GetValueFromQuery(uri.Query, "username");
            Int32.TryParse(GetValueFromQuery(uri.Query, "score"), out info.score);
            Boolean.TryParse(GetValueFromQuery(uri.Query, "reveal"), out info.reveal);
            Int32.TryParse(GetValueFromQuery(uri.Query, "currentgamestate"), out info.currentGameState);

            return info;
        }
        void Listen(object schemeType)
        {
            //listen from all ip sources
            var type = (SchemeTypes)schemeType;

            Socket listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var ipe = new IPEndPoint(IPAddress.Any, schemePorts[type]);
            listenSocket.Bind(ipe);
            log.WriteLine("Started listening with port " + schemePorts[type]);
            log.Flush();
            while (true)
            {

                listenSocket.Listen(10000);
                var activeSocket = listenSocket.Accept();

                int receivedData = 0;
                Byte[] bytes = new byte[256];
                StringBuilder sb = new StringBuilder();
                do
                {
                    receivedData = activeSocket.Receive(bytes, bytes.Length, 0);
                    sb.Append(Encoding.ASCII.GetString(bytes, 0, receivedData));

                } while (receivedData > 0);

                Uri uri = new Uri(sb.ToString());
                log.WriteLine($"{uri.Scheme}, port {schemePorts[type]}");
                log.Flush();
                DataManager(uri);

            }
        }
        void DataManager(Uri uri)
        {
            IPAddress ip = IPAddress.Parse(uri.Host);

            if (uri.Scheme == "conrequest") //acknowledge the message
            {         
                    //send conack
                   SendConAck(ip);               
            }
            else if (uri.Scheme == "conaccept") //accepts game invitation
            {

                bool ret = false;
                foreach (var info in listOfGames)
                {
                    if (info.host == IPAddress.Parse(uri.Host))
                        ret = true;
                }
                if (ret) return;
                if (EngineControl.gameManager.currentGameState == GameManager.GameState.createGame) //in create game and it means some1 is connecting
                    EngineControl.gameManager.currentGame = GetDataFromQuery(uri.OriginalString); //listOfGames.Add(GetDataFromQuery(uri.OriginalString));

            }
            else if (uri.Scheme == "condata") //in game data exchange
            {
                if (ReceivedData.ContainsKey(ip))
                    ReceivedData.Remove(ip);
                var info = GetDataFromQuery(uri.OriginalString);
                EngineControl.gameManager.SetData(info);
                ReceivedData.Add(ip, info);
            }
            else if (uri.Scheme == "conack") 
            {
                //if in create game state, add to list, otherwise do nothing
                bool ret = false;
                foreach (var info in listOfGames)
                {
                    if (info.host == IPAddress.Parse(uri.Host))
                    {
                        //same host sent again with different game state, remove from list
                        if (info.currentGameState != GetDataFromQuery(uri.OriginalString).currentGameState || info.username != GetDataFromQuery(uri.OriginalString).username)
                        {
                            listOfGames.Remove(info);
                        }
                        else
                            ret = true;//to not add it again
                    }
                }
                if (ret) //if already in list then exit, to not add it again if con ack sent again
                    return;
                
                if (GetDataFromQuery(uri.OriginalString).currentGameState == (int)GameManager.GameState.createGame)
                    listOfGames.Add(GetDataFromQuery(uri.OriginalString));


            }
        }
        public static bool HasConnection()
        {
            //ping dg for connection
            if (networkInterface != null)
            {
                Ping ping = new Ping();
                var reply = ping.Send(networkDHCPAddress);
                if (reply.Status == IPStatus.Success)
                    return true;
            }
            return false;
        }
        public static string GetUri(SchemeTypes scheme, IPAddress host, string query = "")
        {
            if (scheme == SchemeTypes.conrequest)
            {
                if (query == "")
                    return $"conrequest://{host.ToString()}";
                else
                    return $"conrequest://{host.ToString()}?{query}";
            }
            if (scheme == SchemeTypes.conaccept)
            {
                if (query == "")
                    return $"conaccept://{host.ToString()}";
                else
                    return $"conaccept://{host.ToString()}?{query}";
            }
            if (scheme == SchemeTypes.condata)
            {
                if (query == "")
                    return $"condata://{host.ToString()}";
                else
                    return $"condata://{host.ToString()}?{query}";
            }
            if (scheme == SchemeTypes.conack)
            {
                if (query == "")
                    return $"conack://{host.ToString()}";
                else
                    return $"conack://{host.ToString()}?{query}";
            }
            return "";
        }

        public void SearchGames()
        {
            if (HasConnection())
            {
                //log.WriteLine("Getting Games");
                //log.Flush();
                // StreamWriter log = new StreamWriter("log1.txt");


                //make it asych, display in option searching...
                //ping all addresses in current network, if success, send conrequest, conrequest returns Info values in query form
                IPAddress hostIp = networkDHCPAddress; //host ip for correct network detection (can be any ip address in active network)

                int thirdDotPos = 0;//for knowing where to remove last digits and add new ones

                List<Info> infos = new List<Info>();
                for (int i = 0; i < hostIp.ToString().Length; i++)
                {
                    if (hostIp.ToString()[i] == '.')
                    {
                        thirdDotPos = i + 1;
                    }

                }

                for (int i = 1; i < 255; i++)//set i to 1
                {
                    //get ip string
                    string ip = hostIp.ToString().Remove(hostIp.ToString().Length - (hostIp.ToString().Length - thirdDotPos));
                    ip += i.ToString();
                    //log.Write(ip+"\n");
                    //log.Flush();
                    SendConRequest(IPAddress.Parse(ip));

                }

            }

        }
        // got reply from conrequest
        Info PingCompletedCallback(PingReply reply)
        {
            if (reply.Status == IPStatus.Success)
                return (new Info { targetIP = reply.Address, username = "default", score = 0, query = "" });
            return Info.Empty();
        }

        public bool SendConRequest(IPAddress ip)
        {
            try
            {
                if (HasConnection())
                {
                    //log.WriteLine("Sending Con req with ip: " + ip.ToString());
                    //log.Flush();
                    Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    IPEndPoint ipe = new IPEndPoint(ip, schemePorts[SchemeTypes.conrequest]);
                    socket.Bind(ipe);
                    socket.Send(ASCIIEncoding.ASCII.GetBytes(GetUri(SchemeTypes.conrequest, ip)));
                    //log.WriteLine("Sent Con req!");
                    //log.Flush();
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
        public bool SendConAccept(IPAddress ip)
        {
            try
            {
                if (HasConnection())
                {
                    //log.WriteLine("Sending Con req with ip: " + ip.ToString());
                    //log.Flush();
                    Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    IPEndPoint ipe = new IPEndPoint(ip, schemePorts[SchemeTypes.conaccept]);
                    socket.Bind(ipe);
                    socket.Send(ASCIIEncoding.ASCII.GetBytes(GetUri(SchemeTypes.conaccept, ip, $"username={EngineControl.gameManager.Username}")));
                    //log.WriteLine("Sent Con req!");
                    //log.Flush();
                    log.WriteLine("Sent con accept");
                    log.Flush();
                    socket.Close();
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
        public bool SendConData(Info info)
        {
            try
            {
                if (HasConnection())
                {
                    Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    IPEndPoint ipe = new IPEndPoint(info.targetIP, schemePorts[SchemeTypes.condata]);
                    socket.Bind(ipe);
                    socket.Send(ASCIIEncoding.ASCII.GetBytes(GetUri(SchemeTypes.condata, info.targetIP, info.query)));
                    socket.Close();
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
        public bool SendConAck(IPAddress ip)
        {
            try
            {
                if (HasConnection())
                {
                    //log.WriteLine("Sending Con req with ip: " + ip.ToString());
                    //log.Flush();
                    Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    IPEndPoint ipe = new IPEndPoint(ip, schemePorts[SchemeTypes.conack]);
                    socket.Bind(ipe);
                    socket.Send(ASCIIEncoding.ASCII.GetBytes(GetUri(SchemeTypes.conack, ip,$"username=${EngineControl.gameManager.Username}&currentgamestate={(int)EngineControl.gameManager.currentGameState}")));
                    //log.WriteLine("Sent Con req!");
                    //log.Flush();
                    socket.Close();
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
        public string[] GetListOfInterfaces()
        {
            NetworkInterface[] ifs = NetworkInterface.GetAllNetworkInterfaces();
            List<string> arr = new List<string>();
            for (int i = 0; i < ifs.Length; i++)
            {
                if (ifs[i].GetIPProperties().GatewayAddresses.Count > 0)
                    arr.Add($"[{ifs[i].Name}] With Gateway Address: {ifs[i].GetIPProperties().GatewayAddresses[0].Address}");

            }

            return arr.ToArray();
        }
        public void SetInterface(int indexOfList)
        {
            NetworkInterface[] ifs = NetworkInterface.GetAllNetworkInterfaces();
            List<NetworkInterface> ifss = new List<NetworkInterface>();
            for (int i = 0; i < ifs.Length; i++)
            {
                if (ifs[i].GetIPProperties().GatewayAddresses.Count > 0)
                    ifss.Add(ifs[i]);

            }
            //if -1 then offline
            if (indexOfList >= 0)
            {
                networkInterface = ifss[indexOfList];
                networkDHCPAddress = ifss[indexOfList].GetIPProperties().GatewayAddresses[0].Address;
            }


        }
        public IPAddress GetIPAddress()
        {
            foreach (var item in networkInterface.GetIPProperties().UnicastAddresses)
            {
                if (item.Address.AddressFamily == AddressFamily.InterNetwork)
                    return item.Address;
            }
            return null;
        }

    }

}
