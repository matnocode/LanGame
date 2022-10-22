using System;
using System.Threading;
namespace ConsoleEngine
{
    public class Controls
    {
        public Controls()
        {
            LookForInput = true;
            
        }
        public bool LookForInput;
        public delegate void ControlsDel(ConsoleKeyInfo obj);
        public event ControlsDel OnInputDetected;
        bool newInput = false;
        ConsoleKeyInfo k;
        public void CheckInput()
        {
            while (LookForInput)
            {
                k = Console.ReadKey(true);
                //OnInputDetected?.Invoke(key);            
                newInput = true;
            }
        }
        public void GetInput() 
        {

            if (newInput)
            {
                OnInputDetected?.Invoke(k);
                //Console.WriteLine("New input! " + k.Key.ToString());
                newInput = false;
            }
        }
        public void initialize() 
        {
            //Console.WriteLine("initialized");
            EngineControl.engine.BeforeLoop += GetInput;
        }

    }
}
