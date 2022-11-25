using System;
using System.Threading;

namespace ConsoleEngine
{
    public class EngineControl
    {
        public static Engine engine;
        public static Graphics graphics;
        public static Controls controls;
        public static LanNetwork lanNetwork;
        public static GameManager gameManager;
        static void Main(string[] args)
        {
            controls = new Controls();
            Thread controlthread = new Thread(new ThreadStart(controls.CheckInput));
            controlthread.Start();
            Initialize();
            //graphics.AddToBuffer(new Vector2(0, 0), "Hello");
            engine.Start();           
        
        }

        public static void Initialize() 
        {
            lanNetwork = new LanNetwork();
            engine = new Engine();
            controls.initialize();
            graphics = new Graphics();
            gameManager = new GameManager();
            
        }
    }
}
