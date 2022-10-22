namespace ConsoleEngine
{
    public class Vector2
    {
        public Vector2()
        {
            x = 0; y = 0;
        }
        public Vector2(int x, int y)
        {
            this.x = x; this.y = y;
        }
        public int x, y;

        public static bool operator ==(Vector2 v1, Vector2 v2)
        {
            if (!v2.Equals(null))
            {
                if (v1.x == v2.x && v1.y == v2.y && !v2.Equals(null))
                    return true;
                return false;
            }
            return false;
        }
        public static bool operator !=(Vector2 v1, Vector2 v2)
        {
            if (!v2.Equals(null))
            {
                if (v1.x == v2.x && v1.y == v2.y)
                    return false;
                return true;
            }
            return true;
        }
        public static Vector2 Right() 
        {
            return new Vector2(1, 0);
        }
        public static Vector2 Left()
        {
            return new Vector2(-1, 0);
        }
        public static Vector2 Up()
        {
            return new Vector2(0, 1);
        }
        public static Vector2 Down()
        {
            return new Vector2(0, -1);
        }
    }
}
