using System.Collections.Generic;
using System.IO;
namespace ConsoleEngine
{
    public class GameObject
    {
        Graphics graphics = EngineControl.graphics;
        public GameObject()
        {
            path = null;
            points = null;
            name = null;
            fileStream = null;
        }
        public GameObject(FileStream fs, string name, Vector2 pos)
        {
            if (fs != null)
            {
                fileStream = fs;
                Compile();
                this.name = name;
                position = pos;
            }
        }
        void Compile()
        {
            StreamReader reader = new StreamReader(fileStream);
            List<string> _lines = new List<string>();

            string temp = " ";
            while (temp != null)
            {
                temp = reader.ReadLine();
                if (temp != null)
                    _lines.Add(temp);
            }

            lines = _lines;

            int pointnum = 0;

            for (int y = 0; y < lines.Count; y++)
            {
                for (int x = 0; x < lines[y].Length; x++)
                {
                    pointnum++;
                }
            }

            points = new Graphics.PointData[pointnum];
            int tempn = 0;

            for (int y = 0; y < lines.Count; y++)
            {
                for (int x = 0; x < lines[y].Length; x++)
                {
                    points[tempn] = new Graphics.PointData(new Vector2(x, y), lines[y][x].ToString());
                    tempn++;
                }
            }
            fileStream.Flush();
            fileStream.Dispose();

        }
        public void RenderGameObject()
        {
            graphics.AddPoint(points);
            
        }
        public void Move(Vector2 newpos)//add values
        {
            if (position != newpos)
            {
             
                Remove();
                Vector2 tempVec = new Vector2();
                Graphics.PointData tempDat = new Graphics.PointData();
                for (int i = 0; i < points.Length; i++)
                {
                    tempVec = points[i].position;
                    tempVec.x += newpos.x;
                    tempVec.y += newpos.y;
                    tempDat = points[i];
                    tempDat.position = new Vector2(tempVec.x, tempVec.y);
                    points[i] = tempDat;
                }
                RenderGameObject();
                position = newpos;
            }
        }
        public void Remove() 
        {
            for (int i = 0; i < points.Length; i++)
            {
                graphics.RemovePoint(points[i].position);
            }
        }
        public int GetWidth() 
        {
            //gets max line lenght
            int maxlenght = 0;
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].Length > maxlenght)
                    maxlenght = lines[i].Length;
            }
            return maxlenght;
        }
        public int GetHeight() 
        {
            return lines.Count;
        }        

        public string path;
        public Graphics.PointData[] points;
        public string name;
        FileStream fileStream;
        public Vector2 worldPosition { get { return this.position; } set { position = value; Move(value); } }
        private Vector2 position;
        public List<string> lines;

    }
}
