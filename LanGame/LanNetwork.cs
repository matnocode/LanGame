﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
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
            conListenThread.Start(SchemeTypes.conrequest);
            accListenThread.Start(SchemeTypes.conaccept);
            dataListenThread.Start(SchemeTypes.condata);
        }
        public static readonly Dictionary<SchemeTypes, int> schemePorts = new Dictionary<SchemeTypes, int>()
        {
            {SchemeTypes.conrequest, 3030 },
            {SchemeTypes.conaccept, 3020 },
            {SchemeTypes.condata, 3010 }
        };
        public enum SchemeTypes
        {
            conrequest, conaccept, condata
        }
        public struct Info
        {
            public static Info Empty()
            {
                return new Info() { empty = true };
            }
            public Info(string uname, IPAddress ipp, int sc = 0, string que = "", bool rev = false)
            {
                this.username = uname;
                this.ip = ipp;
                this.score = sc;
                this.query = que;
                empty = false;
                reveal = rev;
            }
            public bool isEmpty()
            {
                return empty;
            }
            public string username;
            public IPAddress ip;
            public int score;
            public string query;
            public bool reveal;
            bool empty;
        }

        public List<Info> listOfGames;
        public Dictionary<IPAddress, Info> ReceivedData = new Dictionary<IPAddress, Info>();

        //has connection in a local network //ping to default gateway

        //fix
        static string GetValueFromQuery(string query, string dataName)
        {
            if (query.Contains(dataName))
            {
                string dataQuery = query.Substring(query.IndexOf(dataName));
                if (dataQuery.Substring(dataQuery.IndexOf(dataName) + 1 + dataName.Length).Contains("&"))
                {
                    //+1 because of =
                    return dataQuery.Substring(dataQuery.IndexOf(dataName) + 1 + dataName.Length, dataQuery.IndexOf("&", dataQuery.IndexOf(dataName)));
                }
                else
                {
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

            info.ip = IPAddress.Parse(uri.Host);
            info.query = uri.Query;

            info.username = GetValueFromQuery(uri.Query, "username");
            Int32.TryParse(GetValueFromQuery(uri.Query, "score"), out info.score);
            Boolean.TryParse(GetValueFromQuery(uri.Query, "reveal"), out info.reveal);


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

            if (uri.Scheme == "conrequest")
            {
                if (EngineControl.gameManager.currentGameState == GameManager.GameState.createGame  )
                {
                    //send conaccept
                    SendConAccept(ip);
                }
            }
            else if (uri.Scheme == "conaccept")
            {
                bool ret = false;
                foreach (var info in listOfGames)
                {
                    if (info.ip == IPAddress.Parse(uri.Host))
                        ret = true;
                }
                if (ret) return;
                listOfGames.Add(GetDataFromQuery(uri.OriginalString));
            }
            else if (uri.Scheme == "condata")
            {
                if (ReceivedData.ContainsKey(ip))
                    ReceivedData.Remove(ip);
                ReceivedData.Add(ip, GetDataFromQuery(uri.OriginalString));
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
                return (new Info { ip = reply.Address, username = "default", score = 0, query = "" });
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
                    socket.Send(ASCIIEncoding.ASCII.GetBytes(GetUri(SchemeTypes.conrequest, ip,$"username={EngineControl.gameManager.Username}")));
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
                    //log.WriteLine("Sending Con req with ip: " + ip.ToString());
                    //log.Flush();
                    Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    IPEndPoint ipe = new IPEndPoint(info.ip, schemePorts[SchemeTypes.condata]);
                    socket.Bind(ipe);
                    socket.Send(ASCIIEncoding.ASCII.GetBytes(GetUri(SchemeTypes.conaccept, info.ip, info.query)));
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
                    arr.Add($"[{ifs[i].Name}] With Address Gateway: {ifs[i].GetIPProperties().GatewayAddresses[0].Address}");

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
