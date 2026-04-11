namespace AkiGames.Core
{
    public struct Vector2(float x, float y)
    {
        public float X = x, Y = y;

        public static Vector2 Zero => new(0, 0);
        public static Vector2 One => new(1, 1);

        public static Vector2 operator +(Vector2 a, Vector2 b) => new(a.X + b.X, a.Y + b.Y);
        public static Vector2 operator -(Vector2 a, Vector2 b) => new(a.X - b.X, a.Y - b.Y);
        public static Vector2 operator *(Vector2 a, float scalar) => new(a.X * scalar, a.Y * scalar);
        public static Vector2 operator *(float scalar, Vector2 a) => new(a.X * scalar, a.Y * scalar);
    }

    public struct Point(int x, int y)
    {
        public int X = x, Y = y;

        public static Point Empty => new(0, 0);

        public readonly Vector2 ToVector2() => new(X, Y);
    }

    public struct Rectangle(int x, int y, int w, int h)
    {
        public int X = x, Y = y, Width = w, Height = h;

        public static Rectangle Empty => new(0, 0, 0, 0);

        public readonly int Left => X;
        public readonly int Top => Y;
        public readonly int Right => X + Width;
        public readonly int Bottom => Y + Height;
        
        public readonly bool Contains(Point point) => point.X >= X && point.X <= X + Width && point.Y >= Y && point.Y <= Y + Height;
        public readonly bool Contains(int x, int y) => Contains(new Point(x, y));
        
        // Перегрузка операторов
        public static bool operator ==(Rectangle left, Rectangle right) =>
            left.X == right.X && left.Y == right.Y && left.Width == right.Width && left.Height == right.Height;
        public static bool operator !=(Rectangle left, Rectangle right) => !(left == right);
        
        public override readonly bool Equals(object? obj) => obj is Rectangle r && this == r;
        public override readonly int GetHashCode() => HashCode.Combine(X, Y, Width, Height);
    }
    
    public class GameTime
    {
        public TimeSpan TotalGameTime { get; set; }
        public TimeSpan ElapsedGameTime { get; set; }
        public double TotalGameTimeTotalMilliseconds => TotalGameTime.TotalMilliseconds;
    }
}