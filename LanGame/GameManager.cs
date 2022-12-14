using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleEngine
{

    public class GameManager
    {
        public GameManager()
        {
            _opponentname = "bot1";
            LoadUserName();
            currentGameState = GameState.selectInterface;
            currentOptionType = OptionType.selectInterface;
            //Border = new Vector2(2, 1);//offsets
            SetUpLevel();
            EngineControl.engine.BeforeLoop += LevelLogic;
            controls.OnInputDetected += ControlManager;
            OnGameStateChange += SetUpLevel;
            OnGameStateChange += SendData;
            currentGame = LanNetwork.Info.Empty();
        }
        //create event delegate for setting levels
        public delegate void LevelDelegate();
        public event LevelDelegate OnGameStateChange;
        public event LevelDelegate OnDataChange;

        public static ConsoleColor defaultSelectionColor = ConsoleColor.DarkBlue;

        public enum GameState
        {
            mainmenu = 0, loadingscreen0 = 1, pausemenu = 2, ingame = 3, loadingscreen1 = 4 ,
            searchgame = 5, createGame = 6, selectInterface = 7
        }
        public enum GameType
        {
            lan, ai
        }
        enum UIElement { connectivity, username }
        public enum OptionType
        {
            mainmenu, inputTaker, searchGame, createGame,
            ingame, selectInterface
        }
        Dictionary<OptionType, Option[]> options = new Dictionary<OptionType, Option[]>();

        public GameState currentGameState;
        //GameType currentGameType;

        public OptionType currentOptionType;
        Graphics graphics = EngineControl.graphics;
        Controls controls = EngineControl.controls;
        Vector2 Border; // screen border
        int ctrlpos; //position of selection
        int mappos;
        int maxpos; // max pos of selections

        private int _playerScore ;
        public int PlayerScore { get => _playerScore; set { _playerScore = value; OnDataChange?.Invoke(); } }
        private int _opponentScore;
        public int OpponentScore { get => _opponentScore; set { _opponentScore = value; OnDataChange?.Invoke(); } }
        public static string gamename = "LanNumberGuessr";
        private string _username = "";

        public LanNetwork.Info currentGame;

        bool opponentReveal;
        bool _playerReveal;
        public bool PlayerReveal { get { return _playerReveal; } set {_playerReveal=value; OnDataChange?.Invoke(); } }


        public string Username { get => _username; set { _username = value; SetUpUI(); } }
        public string _opponentname;
        public string OpponentName { get => _opponentname; set { _opponentname = value; SetUpUI(); } }
        Dictionary<UIElement, Vector2> UIElementPositions;
        int backOptionPos = 0;//back option postion in option


        //Level Specific variables-------------------
        DateTime Loading0ScreenStartTime = new DateTime();
        DateTime Loading1ScreenStartTime = new DateTime();
        GameObject wfc;
        private bool _sentRequest;
        bool SentRequest
        {
            get => _sentRequest; set
            {
                graphics.ClearWorld();
                _sentRequest = value;
                if (options.ContainsKey(OptionType.createGame) == true)
                { options.Remove(OptionType.createGame); }

                int nextPos = 0;
                List<Option> tempOptArr = new List<Option>();


                tempOptArr.Add(new Option("Enter IP address for the game: (Press enter here)---> ", nextPos));
                nextPos++;
                tempOptArr.Add(new Option("Back", nextPos));
                backOptionPos = nextPos;
                nextPos++;
                if (!_sentRequest)
                    tempOptArr.Add(new Option("No Game Found", nextPos));

                options.Add(OptionType.createGame, tempOptArr.ToArray());

            }
        }

        int _generatedNumber;//3 decimal places
        string GeneratedNumber;

        int currentGuess;


        GameObject map;

        Vector2 PlayerPos = new Vector2();
        Vector2 CameraPos = new Vector2();

        struct Option
        {
            public Option(string val, int pos, Vector2 wp, string[] args = null)
            {
                value = val;
                position = pos;
                worldPosition = wp;
                this.args = args;
            }
            public Option(string val, int pos, string[] args = null)
            {
                value = val;
                position = pos;
                worldPosition = new Vector2(0, 0);
                this.args = args;
            }
            public string value;
            public int position;
            public Vector2 worldPosition;
            public string[] args;

        }


        public void SetGameState(GameState state)
        {
            currentGameState = state;
            OnGameStateChange?.Invoke();
        }
        void SetUpLevel()
        {
            graphics.ClearWorld();
            SetUpOptions();

            if (currentGameState == GameState.mainmenu)
            {
                if (currentOptionType == OptionType.inputTaker)
                {
                    SetUpOptionScreen(10, 2);
                }
                else
                {
                    currentOptionType = OptionType.mainmenu;
                    SetUpOptionScreen(10, 2);

                    //var name = new GameObject(new FileStream("Assets/enginename.cea", FileMode.Open, FileAccess.Read), "enginename", new Vector2(0, 0));
                    var logo = new GameObject(new FileStream("Assets/logo.cea", FileMode.Open, FileAccess.Read), "logo", new Vector2(0, 0));
                    var name = new GameObject(new FileStream("Assets/gamename.cea", FileMode.Open, FileAccess.Read), "logo", new Vector2(0, 0));

                    name.Move(AlignAtCenter(name.GetWidth(), 1));
                    logo.Move(AlignAtCenter(logo.GetWidth(), (GetScreenCenter().y * 2) - 5));
                    name.RenderGameObject();
                    logo.RenderGameObject();
                    SetUpUI();
                    controls.LookForInput = true;
                }
            }
            else if (currentGameState == GameState.loadingscreen0)
            {
                Loading0ScreenStartTime = DateTime.Now;
                var Load0 = new GameObject(new FileStream("Assets/name.cea", FileMode.Open, FileAccess.Read), "load0", new Vector2(0, 0));
                Load0.Move(AlignAtCenter(Load0.GetWidth(), 1));
            }
            else if (currentGameState == GameState.loadingscreen1)
            {
                Loading1ScreenStartTime = DateTime.Now;
                var Load1 = new GameObject(new FileStream("Assets/name1.cea", FileMode.Open, FileAccess.Read), "load1", new Vector2(0, 0));
                //Load1.Move(new Vector2(GetScreenCenter().x - (Load1.GetWidth() / 2), 2));
                Load1.Move(AlignAtCenter(Load1.GetWidth(), 1));
            }

            else if (currentGameState == GameState.ingame)
            {

                _generatedNumber = new Random().Next(100, 999);
                GeneratedNumber = "???";
                Reveal(1);
                Reveal(2);
                currentOptionType = OptionType.ingame;
                _opponentname = currentGame.username;
                _opponentScore = currentGame.score;
                //opponents values
                string opponentVals = _opponentname + " : " + _opponentScore;
                //slightly less than center
                Vector2 oPos = new Vector2(AlignAtCenter(opponentVals, 0).x / 4, 1);

                //players values
                string playerVals = Username + " : " + _playerScore;
                Vector2 pPos = new Vector2(AlignAtCenter(playerVals, 0).x / 4, 3);// x = screen width - (screen width /4)

                //num to guess values
                string numToGuessVals = "Number to guess: " + GeneratedNumber;
                Vector2 ntgPos = new Vector2(AlignAtCenter(numToGuessVals, 0).x, 5);

                //offer values //move to options
                string offer = $"Offer {_opponentname} to reveal a decimal place";
                Vector2 ofPos = new Vector2(AlignAtCenter(offer, 0).x / 4, 9);


                //reveal acceptions
                string revealVal = $"{Username}---->[{GetRevealString(Username)}]    [{GetRevealString(Username)}]<----{_opponentname}";
                Vector2 rPos = new Vector2(AlignAtCenter(offer, 0).x / 4, graphics.GetConsoleHeight() - 2);

                //reveal acceptions
                string revealAskVal = "Reveal Acceptions:";
                Vector2 raPos = new Vector2((AlignAtCenter(offer, 0).x / 4) + revealVal.Length / 4, graphics.GetConsoleHeight() - 3);

                //robot on side
                var sideArt = new GameObject(new FileStream("Assets/sideArt.cea", FileMode.Open, FileAccess.Read), "sideArt", new Vector2());
                sideArt.Move(new Vector2(
                    AlignAtCenter(sideArt.GetWidth(), 0).x + (AlignAtCenter(sideArt.GetWidth(), 0).x / 2 + 15),
                    1));



                //render
                graphics.AddPoint(new Graphics.PointData(oPos, opponentVals));
                graphics.AddPoint(new Graphics.PointData(pPos, playerVals));
                graphics.AddPoint(new Graphics.PointData(ntgPos, numToGuessVals));
                graphics.AddPoint(new Graphics.PointData(raPos, revealAskVal));
                graphics.AddPoint(new Graphics.PointData(rPos, revealVal));
                sideArt.RenderGameObject(); ;

                //set up options because option value changed
                SetUpOptions();
                SetUpOptionScreen(graphics.GetConsoleHeight() - 8, 1);

            }
            else if (currentGameState == GameState.searchgame)
            {
                //to do: looks for games in bg, when done looking for games call callback function to set up what found, otherwise keep array null
                SetUpOptionScreen(4, 2);
                GetAvailableGames();
                SetUpOptionScreen(4, 2);
            }
            else if (currentGameState == GameState.createGame)
            {
               
                if (currentGame.isEmpty())
                {
                    if (wfc == null) wfc = new GameObject(new FileStream("Assets/waitingForConnection.cea", FileMode.Open, FileAccess.Read), "wfc", new Vector2());
                    wfc.Move(AlignAtCenter(wfc.GetWidth(), GetScreenCenter().y - wfc.GetHeight() / 2));
                    wfc.RenderGameObject();

                    string esc = "Press [Escape] to cancel";
                    Vector2 escPos = new Vector2(AlignAtCenter(esc, 0).x, graphics.GetConsoleHeight() - 1);
                    graphics.AddPoint(new Graphics.PointData(escPos, esc));

                    string ip = $"Your Ip Address: {EngineControl.lanNetwork.GetIPAddress()}";
                    Vector2 ipPos = new Vector2(AlignAtCenter(ip, 0).x, graphics.GetConsoleHeight() - 2);
                    graphics.AddPoint(new Graphics.PointData(ipPos, ip));

                }
            }
            else if (currentGameState == GameState.selectInterface)
            {
                graphics.AddPoint(new Graphics.PointData(AlignAtCenter("Please select active network interface", 3), "Please select active network interface"));
                SetUpOptionScreen(10, 2);
            }
        }
        void SetUpUI()
        {
            //run set up if ui value changes
            UIElementPositions = new Dictionary<UIElement, Vector2>();
            StringBuilder sb;
            Vector2 defaultPos;
            ConsoleColor bc, fc;
            //static positions on screen

            //connectivity
            {
                sb = new StringBuilder("Connected: ");
                defaultPos = new Vector2(1, 1);
                fc = ConsoleColor.White;
                if (!LanNetwork.HasConnection())
                    bc = ConsoleColor.Red;
                else
                    bc = ConsoleColor.Green;
                //adds Connected: 
                for (int i = 0; i < sb.Length; i++)
                {
                    graphics.RemovePoint(new Vector2(defaultPos.x + i, defaultPos.y));
                    graphics.AddPoint(new Graphics.PointData(new Vector2(defaultPos.x + i, defaultPos.y), sb[i].ToString(), bc, fc));
                }
                defaultPos.x = sb.Length;
                sb = new StringBuilder(LanNetwork.HasConnection().ToString());
                //adds true or false for connection
                for (int i = 0; i < sb.Length; i++)
                {
                    graphics.RemovePoint(new Vector2(defaultPos.x + i, defaultPos.y));
                    graphics.AddPoint(new Graphics.PointData(new Vector2(defaultPos.x + i, defaultPos.y), sb[i].ToString(), bc, fc));
                }
            }

            //Username
            {
                sb = new StringBuilder("Hello, ");
                defaultPos = AlignAtEnd(sb.ToString() + _username, 1);
                bc = ConsoleColor.Yellow;
                fc = ConsoleColor.Black;
                //adds hello
                for (int i = 0; i < sb.Length; i++)
                {
                    graphics.RemovePoint(new Vector2(defaultPos.x + i, defaultPos.y));
                    graphics.AddPoint(new Graphics.PointData(new Vector2(defaultPos.x + i, defaultPos.y), sb[i].ToString(), bc, fc));
                }
                //adds username
                defaultPos.x += sb.Length;
                sb = new StringBuilder(_username);
                for (int i = 0; i < sb.Length; i++)
                {
                    graphics.RemovePoint(new Vector2(defaultPos.x + i, defaultPos.y));
                    graphics.AddPoint(new Graphics.PointData(new Vector2(defaultPos.x + i, defaultPos.y), sb[i].ToString(), bc, fc));
                }
            }

            //adds score
            if (currentGameState == GameState.ingame)
            {
                sb = new StringBuilder($"{_username} : {_playerScore} - {_opponentScore}:{_opponentname}");
                defaultPos = AlignAtCenter(sb.ToString(), 1);
                //adds score: 
                for (int i = 0; i < sb.Length; i++)
                {
                    graphics.RemovePoint(new Vector2(defaultPos.x + i, defaultPos.y));
                    graphics.AddPoint(new Graphics.PointData(new Vector2(defaultPos.x + i, defaultPos.y), sb[i].ToString()));
                }
            }
        }

        void LevelLogic()
        {
            if (currentGameState == GameState.loadingscreen0)
            {
                //change back to 3
                if (DateTime.Now.Subtract(Loading0ScreenStartTime).Seconds > 2)
                {
                    SetGameState(GameState.loadingscreen1);
                }
            }
            else if (currentGameState == GameState.loadingscreen1)
            {
                //change back to 3
                if (DateTime.Now.Subtract(Loading1ScreenStartTime).Seconds > 2)
                {
                    SetGameState(GameState.mainmenu);
                }
            }
            else if (currentGameState == GameState.ingame)
            {
                if (currentGuess == _generatedNumber)
                {
                    SendData();
                    currentGuess = 0;
                    _generatedNumber = 0;
                    PlayerReveal = false;
                    opponentReveal = false;
                    _playerScore++;

                    SetUpLevel();
                }
            }
            else if (currentGameState == GameState.createGame)
            {
                if (!currentGame.isEmpty())
                {
                    currentGameState = GameState.ingame;    
                }

            }


        }
        //call when chaging options
        void SetMaxPos()
        {
            var fs = File.Open("log.txt", FileMode.OpenOrCreate);
            fs.Write(ASCIIEncoding.ASCII.GetBytes($"{options.ContainsKey(OptionType.searchGame)}"));
            fs.Flush();
            fs.Close();

            int max = 0;
            for (int i = 0; i < options[currentOptionType].Length; i++)
            {
                if (options[currentOptionType][i].position > max)
                    max = options[currentOptionType][i].position;
            }
            maxpos = max;
        }
        void SetUpOptionScreen(int Yoffset, int distance) //y distance b/w options
        {
            //graphics.ClearWorld();
            SetMaxPos();//for options max pos
            ctrlpos = 0;
            var optarr = options[currentOptionType];
            Vector2 temp = new Vector2();
            temp.y = Yoffset;

            for (int i = 0; i < optarr.Length; i++)
            {
                temp = AlignAtCenter(optarr[i].value, temp.y);
                optarr[i].worldPosition = temp;
                if (i == 0)
                {
                    graphics.AddPoint(new Graphics.PointData(temp, optarr[i].value, defaultSelectionColor));
                }
                else
                    graphics.AddPoint(new Graphics.PointData(temp, optarr[i].value));


                temp = new Vector2(temp.x, temp.y + distance);
                graphics.Render();
            }
        }
        void SetUpOptions()
        {
            options = new Dictionary<OptionType, Option[]>(1);

            //mainmenu opts
            options.Add(OptionType.mainmenu, new Option[]
            {
                    new Option("Join game", 0),
                    new Option("Create new game", 1),
                    new Option("Versus A.I", 2),
                    new Option("Exit", 3)
            });

            //user creation options
            options.Add(OptionType.inputTaker, new Option[]
            {
                    new Option("Enter Username", 0),
                    new Option("(Press enter)",1)
            });
            options.Add(OptionType.searchGame, new Option[]
            {
                    new Option("Searching For Games...", 0),
                   
            });
            options.Add(OptionType.createGame, new Option[]
           {

           });
            options.Add(OptionType.ingame, new Option[]
            {
                    new Option($"Offer {_opponentname} to reveal a decimal place", 0),
                    new Option("Enter Your Guess --->", 1),
                    new Option("EXIT", 2)
            });

            var interfaces = EngineControl.lanNetwork.GetListOfInterfaces();
            Option[] opts = new Option[interfaces.Length + 1];
            for (int i = 0; i < opts.Length; i++)
            {
                if (i == opts.Length - 1)
                    opts[i] = new Option("Play offline", i);
                else
                    opts[i] = new Option(interfaces[i], i);

            }
            options.Add(OptionType.selectInterface, opts);

        }

        Vector2 AlignAtCenter(int maxlength, int yoffset)
        {
            return new Vector2(graphics.GetConsoleWidth() / 2 - (maxlength / 2), yoffset);
        }
        Vector2 AlignAtCenter(string input, int y)
        {
            return new Vector2(graphics.GetConsoleWidth() / 2 - (input.Length / 2), y);
        }
        Vector2 AlignAtEnd(string input, int y)
        {
            return new Vector2(graphics.GetConsoleWidth() - input.Length, y);
        }
        Vector2 GetScreenCenter(int yoffset = 0)
        {
            return new Vector2(graphics.GetConsoleWidth() / 2, (graphics.GetConsoleHeight() + yoffset) / 2);
        }
        void ControlManager(ConsoleKeyInfo key)
        {

            //move options
            if (key.Key == ConsoleKey.UpArrow)
            {

                if (ctrlpos > 0 && options[currentOptionType].Length > 0)
                {
                    ChangeOptionSelection(-1, defaultSelectionColor);
                    ctrlpos--;
                }
            }
            else if (key.Key == ConsoleKey.DownArrow)
            {
                if (ctrlpos < maxpos && options[currentOptionType].Length > 0)
                {
                    ChangeOptionSelection(1, defaultSelectionColor);
                    ctrlpos++;
                }
            }
           
            if (currentGameState == GameState.ingame)
            {
                if (currentOptionType != OptionType.inputTaker)
                {
                    if (key.Key == ConsoleKey.Enter)
                    {
                        if (ctrlpos == 0) //offer to reveal
                        {

                        }
                        if (ctrlpos == 1) //inputs num
                        {
                            currentOptionType = OptionType.inputTaker;
                        }
                        if (ctrlpos == 2) //exit
                        {

                        }
                    }
                }
                else //take input
                {
                    string num = Console.ReadLine();
                        if (GetNumber(num)>=0)
                    currentGuess = GetNumber(num);  
                }
            }

            else if (currentGameState == GameState.mainmenu)
            {
                if (currentOptionType == OptionType.mainmenu)
                {
                    if (key.Key == ConsoleKey.Enter)
                    {
                        //checking if need to create new user
                        if (_username == "" && ctrlpos != 3)
                        {
                            currentOptionType = OptionType.inputTaker;
                            SetUpLevel();

                        }
                        else
                        {
                            //join game
                            if (ctrlpos == 0)
                            {

                                currentGameState = GameState.searchgame;
                                currentOptionType = OptionType.searchGame;
                                SetUpLevel();
                            }
                            //create server
                            else if (ctrlpos == 1)
                            {
                                currentGameState = GameState.createGame;
                                currentOptionType = OptionType.createGame;
                                SetUpLevel();

                            }
                            //vs ai
                            else if (ctrlpos == 2)
                            {


                            }
                            //exit
                            else if (ctrlpos == 3)
                                EngineControl.engine.Stop();
                        }
                    }
                }
                else
                {
                    if (key.Key == ConsoleKey.Enter)
                    {
                        //inputs name
                        if (ctrlpos == 1)
                        {
                            string name = Console.ReadLine();
                            SaveUserName(name);
                            currentOptionType = OptionType.mainmenu;
                            SetUpLevel();
                        }
                    }
                }
            }
            else if (currentGameState == GameState.searchgame)
            {
                if (key.Key == ConsoleKey.Enter)
                {
                    if (ctrlpos == backOptionPos)
                    {
                        currentGameState = GameState.mainmenu;
                        currentOptionType = OptionType.mainmenu;
                        SetUpLevel();
                    }

                    //pressed enter for available game
                    else if (ctrlpos != 0 && ctrlpos <= maxpos - 2)
                    {
                        if (EngineControl.lanNetwork.SendConAccept(EngineControl.lanNetwork.listOfGames[ctrlpos - 1].host))
                        {
                            //means succesfully sent con accept
                            currentGameState = GameState.ingame;
                            currentOptionType = OptionType.ingame;                          
                            currentGame = EngineControl.lanNetwork.listOfGames[ctrlpos - 1];//-1 because first option is label, index because its rendered consecutively
                            SetUpLevel();
                        }
                    }
                }

            }
            else if (currentGameState == GameState.createGame)
            {
                //add game creation options later // like digits of number, time limit, guess limit
                if(key.Key == ConsoleKey.Escape) 
                {
                    currentGameState = GameState.mainmenu;
                    currentOptionType = OptionType.mainmenu;
                    SetUpLevel();
                }
            }
            else if (currentGameState == GameState.selectInterface)
            {
                if (key.Key == ConsoleKey.Enter)
                {
                    if (ctrlpos >= 0 && ctrlpos != maxpos)
                    {
                        EngineControl.lanNetwork.SetInterface(ctrlpos);
                        currentGameState = GameState.mainmenu;
                        currentOptionType = OptionType.mainmenu;
                        SetUpLevel();
                    }
                    else if (ctrlpos == maxpos)
                    {
                        EngineControl.lanNetwork.SetInterface(-1);
                        currentGameState = GameState.mainmenu;
                        currentOptionType = OptionType.mainmenu;
                        SetUpLevel();
                    }
                }

            }
        }

        //game-------------------------

        public void LoadUserName()
        {
            FileStream fs;

            if (!File.Exists("Assets/username.txt"))
            {
                fs = File.Create("Assets/username.txt");
                fs.Close();
            }                
         
            _username = File.ReadAllText("Assets/username.txt");

        }
        public void SaveUserName(string name)
        {
            File.WriteAllText("Assets/username.txt", name);
            Username = name;
        }
        public void GetAvailableGames()
        {

            EngineControl.lanNetwork.SearchGames();

            //set up new option screen with list of available games
            //get list of games from server
            var gameList = EngineControl.lanNetwork.listOfGames;

            if (gameList.Count > 0)
            {
                graphics.ClearWorld();
                if (options.ContainsKey(OptionType.searchGame) == true)
                { options.Remove(OptionType.searchGame); }


                List<Option> tempOptArr = new List<Option>();
                int nextPos = 0;
                tempOptArr.Add(new Option("Games Found:", nextPos));
                nextPos++;


                for (int i = 0; i < gameList.Count; i++)
                {
                    tempOptArr.Add(new Option($"{gameList[i].username} : {gameList[i].targetIP}", nextPos, new string[] { i.ToString() }));
                    nextPos++;

                }
                tempOptArr.Add(new Option("Try again", nextPos));
                nextPos++;
                tempOptArr.Add(new Option("Back", nextPos));
                backOptionPos = nextPos;
                nextPos++;
                options.Add(OptionType.searchGame, tempOptArr.ToArray());
            }

            else
            {
                graphics.ClearWorld();
                if (options.ContainsKey(OptionType.searchGame) == true)
                { options.Remove(OptionType.searchGame); }

                options.Add(OptionType.searchGame, new Option[] {
                        new Option("No Games Found",0),
                        new Option("Enter IP Manually",1),
                        new Option("Back",2)
                        });
                backOptionPos = 2;

            }
        }
        public void ChangeOptionSelection(int change, ConsoleColor color)
        {

            //remove current selected, add again but without color
            //remove next one, add again but with color

            Option[] optarr = options[currentOptionType];

            //remove current and add again to remove color
            for (int i = 0; i < optarr[ctrlpos].value.Length; i++)
            {
                graphics.RemovePoint(new Vector2(optarr[ctrlpos].worldPosition.x + i, optarr[ctrlpos].worldPosition.y));
                graphics.AddPoint(new Graphics.PointData(new Vector2(optarr[ctrlpos].worldPosition.x + i, optarr[ctrlpos].worldPosition.y), optarr[ctrlpos].value[i].ToString()));
            }
            //StreamWriter sw = new StreamWriter("log.txt");

            for (int i = 0; i < optarr[ctrlpos + change].value.Length; i++)
            {
                graphics.RemovePoint(new Vector2(optarr[ctrlpos + change].worldPosition.x + i, optarr[ctrlpos + change].worldPosition.y));
                graphics.AddPoint(new Graphics.PointData(new Vector2(optarr[ctrlpos + change].worldPosition.x + i, optarr[ctrlpos + change].worldPosition.y), optarr[ctrlpos + change].value[i].ToString(), bc: ConsoleColor.Green));
                //sw.Write("Added Point: value {0}, x:{1}, y:{2}, color:{3} \n", optarr[ctrlpos + change].value[i], optarr[ctrlpos + change].worldPosition.x + i, optarr[ctrlpos + change].worldPosition.y,color);
            }
            //sw.Write("\n--------------------------------------------------------------------------------" +
            //    "\n \n \n");
            //sw.Flush();
            //sw.Dispose();

        }
        public int GetNumber(string num)
        {
            try 
            {
                int n = Int32.Parse(num);
                return n;   
            }
            catch 
            {
                return -1;
            }
        }

        //decimal place reversed, from higher to lower: 3rd = 1 0 0<--
        void Reveal(int decimalPlaceToReveal)
        {
            //minus for indexes
            decimalPlaceToReveal--;
            char[] tempG = GeneratedNumber.ToCharArray();
            tempG[decimalPlaceToReveal] = _generatedNumber.ToString()[decimalPlaceToReveal];
            GeneratedNumber = "";
            foreach(char c in tempG) 
            {
                GeneratedNumber += c.ToString();
            }

        }
        string GetRevealString(string name)
        {
            if (name == Username)
            {
                if (_playerReveal)
                    return "V";
                return "X";
            }
            else
            {
                if (opponentReveal)
                    return "V";
                return "X";
            }
        }

        void SendData() 
        {
            //EngineControl.lanNetwork.Ii
            LanNetwork.Info info = new LanNetwork.Info(Username, currentGame.host, hostIP:EngineControl.lanNetwork.GetIPAddress(),_generatedNumber, (int)currentGameState,PlayerScore, rev:_playerReveal);
            EngineControl.lanNetwork.SendConData(info);

        }
        public void SetData(LanNetwork.Info info) 
        {
            currentGame = info; 
        }

    }


}
