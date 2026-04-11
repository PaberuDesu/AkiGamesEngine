namespace AkiGames.Core
{
    public struct Color(byte r, byte g, byte b, byte a)
    {
        public byte R = r, G = g, B = b, A = a;

        // Упакованное значение в формате ARGB
        public uint PackedValue
        {
            readonly get => (uint)((A << 24) | (R << 16) | (G << 8) | B);
            set
            {
                A = (byte)(value >> 24);
                R = (byte)(value >> 16);
                G = (byte)(value >> 8);
                B = (byte)value;
            }
        }
        public static Color FromPackedValue(uint packed) =>
            new() { PackedValue = packed };

        public static Color White => new(255,255,255,255);
        public static Color Black => new(0,0,0,255);
        public static Color Transparent => new(0,0,0,0);
        public static Color Gray => new(128, 128, 128, 255);
        public static Color DarkGray => new(169, 169, 169, 255);
        public static Color LightGray => new(211, 211, 211, 255);
    
        public static bool operator ==(Color left, Color right) =>
            left.R == right.R && left.G == right.G && left.B == right.B && left.A == right.A;
        public static bool operator !=(Color left, Color right) => !(left == right);

        public override readonly bool Equals(object? obj) => obj is Color other && this == other;
        public override readonly int GetHashCode() => HashCode.Combine(R, G, B, A);
    }
}