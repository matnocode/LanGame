using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace ConsoleEngine
{

    public class GameManager
    {
        public GameManager()
        {
            LoadUserName();
            currentGameState = GameState.loadingscreen0;
            Border = new Vector2(2, 1);//offsets
            SetUpOptions();
            //SetUpBorder();
            SetUpLevel();
            EngineControl.engine.BeforeLoop += LevelLogic;
            controls.OnInputDetected += ControlManager;
            OnGameStateChange += SetUpLevel;
            
        }
        //create event delegate for setting levels
        public delegate void LevelDelegate();
        public event LevelDelegate OnGameStateChange;

        public static ConsoleColor defaultSelectionColor = ConsoleColor.DarkBlue;

        public enum GameState
        {
            mainmenu, loadingscreen0, pausemenu, ingame, loadingscreen1,
            searchgame
        }
        public enum GameType 
        {
            lan,ai
        }
        enum UIElement {connectivity,username }
        enum OptionType
        {
            mainmenu, userCreation, searchGame
        }
        Dictionary<OptionType, Option[]> options = new Dictionary<OptionType, Option[]>();

        GameState currentGameState;
        GameType currentGameType;
        
        OptionType currentOptionType;
        Graphics graphics = EngineControl.graphics;
        Controls controls = EngineControl.controls;
        Vector2 Border; // screen border
        int ctrlpos; //position of selection
        int mappos;
        int maxpos; // max pos of selections

        private int _playerScore;
        public int PlayerScore { get => _playerScore; set { _playerScore = value; SetUpUI(); } }
        private int _opponentScore;
        public int OpponentScore { get => _opponentScore; set { _opponentScore = value; SetUpUI(); } }
        public static string gamename = "LanNumberGuessr";
        private string _username = "";
        public string Username { get => _username; set { _username = value; SetUpUI(); } }
        public string _opponentname = "";
        public string OpponentName { get => _opponentname; set { _opponentname = value; SetUpUI(); } }
        Dictionary<UIElement, Vector2> UIElementPositions;
        int backOptionPos = 0;//back option postion in option


        //Level Specific variables-------------------
        DateTime Loading0ScreenStartTime = new DateTime();
        DateTime Loading1ScreenStartTime = new DateTime();

        GameObject map;

        Vector2 PlayerPos = new Vector2();
        Vector2 CameraPos = new Vector2();

        struct Option
        {
            public Option(string val, int pos, Vector2 wp)
            {
                value = val;
                position = pos;
                worldPosition = wp;
            }
            public Option(string val, int pos)
            {
                value = val;
                position = pos;
                worldPosition = new Vector2(0, 0);
            }
            public string value;
            public int position;
            public Vector2 worldPosition;

        }
      

        public void SetGameState(GameState state)
        {
            currentGameState = state;
            OnGameStateChange?.Invoke();
        }
        void SetUpLevel()   
        {
            graphics.ClearWorld();
          
            if (currentGameState == GameState.mainmenu)
            {
                if (currentOptionType == OptionType.userCreation) 
                {
                    SetUpOptionScreen(10, 2);
                }
                else 
                {
                    currentOptionType = OptionType.mainmenu;
                    SetUpOptionScreen(10, 2);

                    //var name = new GameObject(new FileStream("Assets/enginename.cea", FileMode.Open, FileAccess.Read), "enginename", new Vector2(0, 0));
                    var logo = new GameObject(new FileStream("Assets/robot_0.cea", FileMode.Open, FileAccess.Read), "logo", new Vector2(0, 0));
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
                var Load0 = new GameObject(new FileStream("Assets/name.cea", FileMode.Open,FileAccess.Read), "load0",new Vector2(0,0));
                Load0.Move(AlignAtCenter(Load0.GetWidth(), 1));
            }
            else if (currentGameState == GameState.loadingscreen1)
            {
                Loading1ScreenStartTime = DateTime.Now;
                var Load1 = new GameObject(new FileStream("Assets/name1.cea", FileMode.Open, FileAccess.Read), "load1",new Vector2(0,0));
                //Load1.Move(new Vector2(GetScreenCenter().x - (Load1.GetWidth() / 2), 2));
                Load1.Move(AlignAtCenter(Load1.GetWidth(), 1));
            }
   
            else if (currentGameState == GameState.ingame)
            {
                
            }
            else if (currentGameState == GameState.searchgame)
            {
                SetUpOptionScreen(10, 2);
                GetAvailableGames();
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
            if(currentGameState == GameState.ingame) 
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
                if (DateTime.Now.Subtract(Loading0ScreenStartTime).Seconds > 0)
                {
                    SetGameState(GameState.loadingscreen1);
                }
            }
            else if (currentGameState == GameState.loadingscreen1)
            {
                //change back to 3
                if (DateTime.Now.Subtract(Loading1ScreenStartTime).Seconds > 0)
                {
                    SetGameState(GameState.mainmenu);
                }
            }
            else if (currentGameState == GameState.ingame) 
            {
          
            }
        }
        //call when chaging options
        void SetMaxPos()
        {
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
            graphics.ClearWorld();
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
                    graphics.AddPoint(new Graphics.PointData(temp, optarr[i].value,defaultSelectionColor));
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
            options.Add(OptionType.userCreation, new Option[]
            {
                    new Option("Enter Username", 0),
                    new Option("(Press enter)",1)
            });
            options.Add(OptionType.searchGame, new Option[]
            {
                    new Option("Searching For Game...", 0),
            });


        }
   
        Vector2 AlignAtCenter(int maxlength, int yoffset) 
        {
            return new Vector2(graphics.GetConsoleWidth() / 2  - (maxlength / 2), yoffset);
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
            if (currentGameState == GameState.mainmenu)
            {
                if (key.Key == ConsoleKey.UpArrow)
                {

                    if (ctrlpos > 0)
                    {
                        ChangeOptionSelection(-1, defaultSelectionColor);
                        ctrlpos--;
                    }
                }
                else if (key.Key == ConsoleKey.DownArrow)
                {
                    if (ctrlpos < maxpos)
                    {
                        ChangeOptionSelection(1, defaultSelectionColor);
                        ctrlpos++;
                    }
                }

                if (currentOptionType == OptionType.mainmenu)
                {
                    if (key.Key == ConsoleKey.Enter)
                    {
                        //checking if need to create new user
                        if (_username == "" && ctrlpos != 3)
                        {
                            currentOptionType = OptionType.userCreation;
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
                    if (key.Key == ConsoleKey.UpArrow)
                    {

                        if (ctrlpos > 0)
                        {
                            ChangeOptionSelection(-1, defaultSelectionColor);
                            ctrlpos--;
                        }
                    }
                    else if (key.Key == ConsoleKey.DownArrow)
                    {
                        if (ctrlpos < maxpos)
                        {
                            ChangeOptionSelection(1, defaultSelectionColor);
                            ctrlpos++;
                        }
                    }
                    else if (key.Key == ConsoleKey.Enter)
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
                //dsiplay available games

                if (key.Key == ConsoleKey.UpArrow)
                {

                    if (ctrlpos > 0)
                    {
                        ChangeOptionSelection(-1, defaultSelectionColor);
                        ctrlpos--;
                    }
                }
                else if (key.Key == ConsoleKey.DownArrow)
                {
                    if (ctrlpos < maxpos)
                    {
                        ChangeOptionSelection(1, defaultSelectionColor);
                        ctrlpos++;
                    }
                }
                else if(key.Key == ConsoleKey.Enter) 
                {
                    if(ctrlpos == backOptionPos)
                    {
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
            if (!File.Exists("Assets/username.txt"))
                File.Create("Assets/username.txt");
            string a = File.ReadAllText("Assets/username.txt");
            _username = a;
            
        }
        public void SaveUserName(string name)
        {      
            File.WriteAllText("Assets/username.txt",name);
            Username = name;
        }
        public void GetAvailableGames()
        {
            LanNetwork.Info[] gameList;
            //set up new option screen with list of available games
            //get list of games from server
            if (EngineControl.lanNetwork.listOfGames != null)
                gameList = EngineControl.lanNetwork.listOfGames.ToArray();
            else gameList = EngineControl.lanNetwork.GetListOfAvailableGames();
           
            if (gameList != null) 
            {
                if (options.ContainsKey(OptionType.searchGame) == true)
                { options.Remove(OptionType.searchGame); }

               
                    Option[] tempOptArr = new Option[gameList.Length];
                    for (int i = 0; i < gameList.Length; i++)
                    {
                        tempOptArr[i] = new Option($"{gameList[i].username} : {gameList[i].ip}", i);
                    }
                
            }

            else
            {
                if (options.ContainsKey(OptionType.searchGame) == true)
                { options.Remove(OptionType.searchGame); }
                
                options.Add(OptionType.searchGame, new Option[] {
                    new Option("No Games Found",0),
                    new Option("Try again",1),
                    new Option("Back",2)
                    });
                backOptionPos = 2;
                
            }
        }
        public void ChangeOptionSelection(int change ,ConsoleColor color)
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
                graphics.AddPoint(new Graphics.PointData(new Vector2(optarr[ctrlpos + change].worldPosition.x + i, optarr[ctrlpos + change].worldPosition.y), optarr[ctrlpos + change].value[i].ToString(),bc:ConsoleColor.Green));   
                //sw.Write("Added Point: value {0}, x:{1}, y:{2}, color:{3} \n", optarr[ctrlpos + change].value[i], optarr[ctrlpos + change].worldPosition.x + i, optarr[ctrlpos + change].worldPosition.y,color);
            }
            //sw.Write("\n--------------------------------------------------------------------------------" +
            //    "\n \n \n");
            //sw.Flush();
            //sw.Dispose();

        }
        public bool IsNumber(ConsoleKey key)
        {
            if (key == ConsoleKey.D1 || key == ConsoleKey.D2 || key == ConsoleKey.D3 ||
                key == ConsoleKey.D4 || key == ConsoleKey.D5 || key == ConsoleKey.D6 ||
                key == ConsoleKey.D7 || key == ConsoleKey.D8 || key == ConsoleKey.D9 ||
                key == ConsoleKey.D0)
                return true;
            return false;
        }

    }

   
}
