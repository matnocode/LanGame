using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Linq;
using System.Text;
using System;
using System.Threading;
using System.Collections.Generic;
using System.IO;

namespace ConsoleEngine
{
    public class LanNetwork
    {
        public LanNetwork()
        {
            listOfGames = new List<Info>();
            ThreadStart ts = new ThreadStart(Listen);
            Thread thread = new Thread(ts);
            thread.Start();
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
            public Info(string uname, IPAddress ipp, int sc = 0, string que = "")
            {
                this.username = uname;
                this.ip = ipp;
                this.score = sc;
                this.query = que;
            }
            public string username;
            public IPAddress ip;
            public int score;
            public string query;
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
            IPAddress dg =  NetworkInterface
         .GetAllNetworkInterfaces()
         .Where(n => n.OperationalStatus == OperationalStatus.Up)
         .Where(n => n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
         .SelectMany(n => n.GetIPProperties()?.GatewayAddresses)
         .Select(g => g?.Address)
         .Where(a => a != null)
         .FirstOrDefault();

            using Ping ping = new Ping();
            PingReply reply = ping.Send(dg);
            if (reply.Status == IPStatus.Success)
                return true;
            return false;
        }
        public static string GetUri(SchemeTypes scheme, IPAddress host, string query = "") 
        {
            if(scheme == SchemeTypes.conrequest) 
            {
                if (query == "")
                    return $"conrequest://{host.ToString()}";
                else
                    return $"conrequest://{host.ToString()}?{query}";
            }
            return "";
        }
        public Info[] GetListOfAvailableGames() 
        {
            StreamWriter file = new StreamWriter("log1.txt");
            

            //make it asych, display in option searching...
            //ping all addresses in current network, if success, send conrequest, conrequest returns Info values in query form
            IPAddress hostIp = null;
            foreach (var ipInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if(ipInterface.OperationalStatus == OperationalStatus.Up) 
                {
                   
                    foreach (var ip in ipInterface.GetIPProperties().DnsAddresses)
                    {
                        if (ip.AddressFamily == AddressFamily.InterNetwork)
                            hostIp = ip;
                    }
                }
            }
            int thirdDotPos = 0;//for knowing where to remove last digits and add new ones
            IPAddress tempIp; //for pinging
            Ping ping = new Ping();
            List<Info> infos = new List<Info>();
            for (int i = 0; i < hostIp.ToString().Length; i++)
            {
                if (hostIp.ToString()[i] == '.') 
                {
                    thirdDotPos = i + 1;//fix later
                }

            }          
            
            for (int i = 1; i < 254; i++)
            {
                //get ip string
                string ip = hostIp.ToString().Remove(hostIp.ToString().Length - (hostIp.ToString().Length - thirdDotPos));
                ip += i.ToString();

                tempIp = Dns.GetHostAddresses(ip)[0];
                file.WriteLine($"Ip {tempIp}");
                file.Flush();
                file.WriteLine("Ping Sent");
                file.Flush();
                var reply = ping.Send(tempIp,1);

                if(reply.Status == IPStatus.Success) 
                {
                    file.WriteLine("Ping succesfull");
                    file.Flush();
                    Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    IPEndPoint ipe = new IPEndPoint(tempIp, schemePorts[SchemeTypes.conrequest]);
                    try
                    {
                        socket.Bind(ipe);
                    }
                    catch (Exception) { }
                    if (socket.Connected)
                    {
                        file.WriteLine("connected succesfully, sending conrequest");
                        file.Flush();
                        //sends conrequest
                        socket.Send(Encoding.ASCII.GetBytes(GetUri(SchemeTypes.conrequest, tempIp)));
                        //make listener other app
                    }
                }
                file.WriteLine("---------------------------------------");
                file.Flush();
            }
            file.Close();
            file.Dispose();
            return listOfGames.ToArray();
        }
  
    }

   
}
