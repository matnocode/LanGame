using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Linq;
using System.Text;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace ConsoleEngine
{
    public class LanNetwork
    {
        StreamWriter log = new StreamWriter("log1.txt");
        public delegate void GameListHandler();
        public event GameListHandler OnGameListChange;
        ThreadStart getGamesThreadStart;
        Thread getGamesThread;

        public LanNetwork()
        {
            listOfGames = new List<Info>();
            getGamesThreadStart = new ThreadStart(GetListOfAvailableGames);
            ThreadStart listenThreadStart = new ThreadStart(Listen);

            getGamesThread = new Thread(getGamesThreadStart);
            Thread listenThread = new Thread(listenThreadStart);
            getGamesThread.Start();
            
        }
        public static readonly Dictionary<SchemeTypes, int> schemePorts = new Dictionary<SchemeTypes, int>()
        {
            {SchemeTypes.conrequest, 3020 }
        };
        public enum SchemeTypes
        {
            conrequest
        }
        public struct Info
        {
            public static Info Empty() 
            {
                return new Info() { empty = true};
            }
            public Info(string uname, IPAddress ipp, int sc = 0, string que = "")
            {
                this.username = uname;
                this.ip = ipp;
                this.score = sc;
                this.query = que;
                empty = false;
            }
            public bool isEmpty() 
            {
                return empty;
            }
            public string username;
            public IPAddress ip;
            public int score;
            public string query;
            bool empty;
        }

        public List<Info> listOfGames;
        public Dictionary<IPAddress, Info> ReceivedData = new Dictionary<IPAddress, Info>();

        //has connection in a local network //ping to default gateway
        public static Info GetDataFromQuery(string uriString)
        {
            Uri uri = new Uri(uriString);
            string query = uri.Query;
            Info info = new Info
            {             
                ip = Dns.GetHostAddresses(uri.Host)[0],
                username = query.Substring(query.IndexOf("username=") + 9, query.IndexOf('&', query.IndexOf("username=") + 9)),
                query = uri.Query,
                score = Int16.Parse(query.Substring(query.IndexOf("score=") + 6, query.IndexOf('&', query.IndexOf("score=") + 6)))

            };
            return info;
        }
        void Listen()
        {
            //listen from all ip sources


            Socket listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            var ipe = new IPEndPoint(IPAddress.Any, schemePorts[SchemeTypes.conrequest]);
            listenSocket.Bind(ipe);
            log.Write("Started listening");
            log.Flush();
            while (true)
            {

                listenSocket.Listen(10);
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

                if (uri.Scheme == "conRequest")
                {
                    if (ReceivedData.ContainsKey(Dns.GetHostAddresses(uri.Host)[0]))
                        ReceivedData.Remove(Dns.GetHostAddresses(uri.Host)[0]);
                    Info info = GetDataFromQuery(sb.ToString());
                    ReceivedData.Add(Dns.GetHostAddresses(uri.Host)[0], info);
                }
            }
        }
        public static bool HasConnection()
        {
            IPAddress dg = NetworkInterface
         .GetAllNetworkInterfaces()
         .Where(n => n.OperationalStatus == OperationalStatus.Up)
         .Where(n => n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
         .SelectMany(n => n.GetIPProperties()?.GatewayAddresses)
         .Select(g => g?.Address)
         .Where(a => a != null)
         .FirstOrDefault();
            
            using Ping ping = new Ping();
            PingReply reply;
            try
            {
                reply = ping.Send(dg);
            }
            catch { return false; }
            if (reply.Status == IPStatus.Success)
                return true;
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
            return "";
        }
        public bool SearchGames() 
        {
            if (getGamesThread.IsAlive == false)
            {
                getGamesThread.Start();
                return true;
            }
            return false;
        }
        void GetListOfAvailableGames()
        {
            if (HasConnection())
            {
                log.WriteLine("Getting Games");
                log.Flush();
               // StreamWriter log = new StreamWriter("log1.txt");


                //make it asych, display in option searching...
                //ping all addresses in current network, if success, send conrequest, conrequest returns Info values in query form
                IPAddress hostIp = null;
                foreach (var ipInterface in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ipInterface.OperationalStatus == OperationalStatus.Up)
                    {

                        foreach (var ip in ipInterface.GetIPProperties().DnsAddresses)
                        {
                            if (ip.AddressFamily == AddressFamily.InterNetwork)
                                hostIp = ip;
                        }
                    }
                }
                int thirdDotPos = 0;//for knowing where to remove last digits and add new ones

                List<Info> infos = new List<Info>();
                for (int i = 0; i < hostIp.ToString().Length; i++)
                {
                    if (hostIp.ToString()[i] == '.')
                    {
                        thirdDotPos = i + 1;
                    }

                }

                for (int i = 237; i < 241; i++)//set i to 1
                {
                    //get ip string
                    string ip = hostIp.ToString().Remove(hostIp.ToString().Length - (hostIp.ToString().Length - thirdDotPos));
                    ip += i.ToString();
                    log.Write(ip+"\n");
                    log.Flush();
                    var reply = SendPing(IPAddress.Parse(ip));
                    Info info = PingCompletedCallback(reply);
                    if(info.isEmpty() == false) 
                    {
                        //move to listen and add only from there
                        infos.Add(info);
                    }

                }

                listOfGames = infos;

            }
       
        }
        public void searchGame(IPAddress ip)
        {
            Ping ping = new Ping();
            var reply = ping.Send(ip, 1000);
            
            foreach(var game in listOfGames)
            {
                if(game.isEmpty() != false) 
                {
                    if (game.ip == ip)
                        return;
                }               
            }
            //send con request

        }
         PingReply SendPing(IPAddress ip) 
        {
            Ping ping = new Ping();
            var reply = ping.Send(ip,1000);
            return reply;
        
        }

        Info PingCompletedCallback(PingReply reply) 
        {
            if(reply.Status == IPStatus.Success)
                return(new Info { ip = reply.Address, username = "default", score=0,query=""});
            return Info.Empty();
        }
     
        public bool SendConRequest(IPAddress ip)
        {
            try
            {
                log.WriteLine("Sending Con req with ip: " + ip.ToString());
                log.Flush();
                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint ipe = new IPEndPoint(ip, schemePorts[SchemeTypes.conrequest]);
                socket.Bind(ipe);
                socket.Send(ASCIIEncoding.ASCII.GetBytes(GetUri(SchemeTypes.conrequest, ip)));
                log.WriteLine("Sent Con req!");
                log.Flush();
                return true;
            }
            catch
            {
                return false;
            }
        }


    }
}
