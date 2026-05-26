namespace Match3.Core
{
    public enum GemType
    {
        Red,     // 🔴
        Blue,    // 🔵
        Green,   // 🟢
        Yellow,  // 🟡
        Purple,  // 🟣
        Count    // 타입 개수 (5종)
    }

    public struct TilePosition
    {
        public int Row;
        public int Col;

        public TilePosition(int row, int col)
        {
            Row = row;
            Col = col;
        }

        public override string ToString() => $"({Row}, {Col})";

        public bool IsAdjacentTo(TilePosition other)
        {
            int dr = System.Math.Abs(Row - other.Row);
            int dc = System.Math.Abs(Col - other.Col);
            return (dr == 1 && dc == 0) || (dr == 0 && dc == 1);
        }
    }

    public class Tile
    {
        public GemType Type { get; set; }
        public TilePosition Position { get; set; }
        public bool IsEmpty => Type == GemType.Count;

        public Tile(GemType type, TilePosition position)
        {
            Type = type;
            Position = position;
        }

        public Tile Clone() => new Tile(Type, Position);
    }
}
