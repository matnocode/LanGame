using System;
using System.Collections.Generic;

namespace ConsoleEngine
{

    public class Graphics
    {


        public delegate void GraphicsDelegate();
        public event GraphicsDelegate OnResolutionChange;
        public const ConsoleColor defaultbcolor = ConsoleColor.Black;
        public const ConsoleColor defaultfcolor = ConsoleColor.White;

        public Graphics()
        {
            //set buffer params to window
            WorldPoints = new List<PointData>();

            CameraPosition = new Vector2(0, 0);
            EngineControl.engine.BeforeLoop += CheckConsoleRes;
            EngineControl.engine.AfterLoop += Render;
            SetRenderRes(new Vector2(120, 30));
            SetConsoleRes(RenderResolution);
            RenderPoints = new List<PointData>(RenderResolution.x * RenderResolution.y);
            ResetConsoleBuffer();
            Console.ResetColor();


            Console.CursorVisible = false;
            OnResolutionChange += ResetConsoleBuffer;

            // AddPoint(new Graphics.PointData(new Vector2(0, 0), "s"));
        }


        public int GetConsoleWidth()
        {
            return Console.WindowWidth;
        }
        public int GetConsoleHeight()
        {
            return Console.WindowHeight;
        }
        public bool SetConsoleRes(Vector2 res)
        {
            try
            {
                Console.SetWindowSize(res.x, res.y);
            }
            catch (Exception e)
            {
                //write to logs
                return false;
            }
            OnResolutionChange?.Invoke();
            return true;
        }
        public bool SetRenderRes(Vector2 res)
        {
            if (res.x <= GetConsoleWidth() && res.y <= GetConsoleHeight())
            {
                RenderResolution = res;
                return true;
            }
            return false;
        }
        void CheckConsoleRes()
        {

        }
        void ResetConsoleBuffer()
        {
            Console.BufferHeight = Console.WindowHeight;
            Console.BufferWidth = Console.WindowWidth;
        }

        //Rendering------------------------------------------------------

        Vector2 RenderResolution;
        Vector2 CameraPosition;
        public struct PointData
        {
            public PointData(Vector2 pos, string val, ConsoleColor bc = Graphics.defaultbcolor, ConsoleColor fc = Graphics.defaultfcolor)
            {
                position = pos;
                value = val;
                bcolor = bc;
                fcolor = fc;
            }
            public Vector2 position;
            public ConsoleColor bcolor;
            public ConsoleColor fcolor;
            public string value;
        }
        List<PointData> WorldPoints;
        List<PointData> RenderPoints;
        public void Render()
        {
            Console.ResetColor();
            //world position has to be less than or equal to render resolution + camera offset
            for (int i =  0; i < WorldPoints.Count; i++)
            {
                if(WorldPoints[i].position.x < RenderResolution.x + CameraPosition.x && WorldPoints[i].position.x >= CameraPosition.x &&
                   WorldPoints[i].position.y < RenderResolution.y + CameraPosition.y && WorldPoints[i].position.y >= CameraPosition.y)
                {
                    Console.SetCursorPosition(WorldPoints[i].position.x - CameraPosition.x, WorldPoints[i].position.y - CameraPosition.y);
                    Console.BackgroundColor = WorldPoints[i].bcolor;
                    Console.ForegroundColor = WorldPoints[i].fcolor;
                    Console.Write(WorldPoints[i].value);
                }
            }
        }

        public void AddPoint(PointData pointData)
        {
            if(pointData.position.x >= 0 && pointData.position.y >= 0) 
            {
                WorldPoints.Add(pointData);
            }
        }
        public void AddPoint(PointData[] pointDatas)
        {
            //every game objecgt will have point data array
            for (int i = 0; i < pointDatas.Length; i++)
            {
                if (pointDatas[i].position.x >= 0 && pointDatas[i].position.y >= 0)
                {
                    WorldPoints.Add(pointDatas[i]);
                }
            }
        }
        public void RemovePoint(Vector2 pos)
        {
            for (int i = 0; i < WorldPoints.Count; i++)
            {
                if (WorldPoints[i].position == pos)
                    WorldPoints.RemoveAt(i);
            }
        }

        public void ClearWorld()
        {
            Console.ResetColor();
            Console.Clear();
            Console.CursorVisible = false;
            WorldPoints = new List<PointData>();
        }

        public void MoveCamera(Vector2 pos)
        {
            if (!pos.Equals(null))
            {
                if (pos.x >= 0 && pos.y >= 0)
                {
                    CameraPosition = new Vector2(pos.x + CameraPosition.x, pos.y + CameraPosition.y);
                    //Compile();
                }
            }
        }
    }
}
