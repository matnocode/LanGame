using System;
using System.IO;
using System.Threading;
namespace ConsoleEngine
{
    public class Engine 
    {
        public delegate void EngineDelegate();
        public event EngineDelegate BeforeFirstLoop;
        public event EngineDelegate BeforeLoop;
        public event EngineDelegate AfterLoop;
        
        public bool loop;
        int fps;

        public void Start() 
        {
            loop = true;
            fps = 85; //10 fps, set this number to change fps
            BeforeFirstLoop?.Invoke();
            Loop();
        }

        public void Loop() 
        {
            DateTime start = new DateTime();
            int substract = 0;
            while (loop)
            {
                start = DateTime.Now;
                BeforeLoop?.Invoke();
                AfterLoop?.Invoke();
                Thread.Sleep(fps);
                //Thread.Sleep(fps - ((int)DateTime.Now.Subtract(start).TotalMilliseconds)); //- elapsed time
            }
        }

        public void Stop() 
        {
            loop = false;
            EngineControl.controls.LookForInput = false;
        }

        public FileStream GetFile(string fileNamePathName, bool create=false) 
        {
            //opens or creates file from root
            if (create)
                return File.Open(fileNamePathName, FileMode.OpenOrCreate);
           
            else
            {
                //opens a specific file from root or from assets if not found returns null
                try
                {
                    return File.Open(fileNamePathName, FileMode.Open);
                }
                catch (Exception)
                {
                    try 
                    {
                        return File.Open("assets\\"+fileNamePathName, FileMode.Open);                      
                    }
                    catch (Exception) 
                    { 
                       return null;//not found
                    }             
                }
            }
           
        }
    
    }
}
