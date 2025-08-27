namespace CapYap.Utils.Models
{
    public class Bounds
    {
        public int Left { get; } = int.MaxValue;
        public int Top { get; } = int.MaxValue;
        public int Right { get; } = int.MinValue;
        public int Bottom { get; } = int.MinValue;

        public Bounds(int left, int top, int right, int bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }
    }
}
