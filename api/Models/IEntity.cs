namespace api.Models
{
    public abstract class IEntity(int x, int y)
    {
        public int X { get; set; } = x;
        public int Y { get; set; } = y;
        public abstract string ToData();
    }

    public class SnakeSegment(int x, int y, string direction, string type, int playerNumber) : IEntity(x, y)
    {
        public int PlayerNumber { get; set; } = playerNumber;
        public string Direction { get; set; } = direction;
        public string Type { get; set; } = type;

        public override string ToData()
        {
            return $"snake{PlayerNumber}-{Type}-{Direction}";
        }

        public override string ToString()
        {
            return ToData() + "-" + X + "-" + Y;
        }
    }

    public class Apple(int x, int y) : IEntity(x, y)
    {
        public int movesLeft = Game.APPLE_LIFESPAN;

        public virtual int Eat()
        {
            return movesLeft > Game.ROTTEN_APPLE_LIFESPAN ? Game.APPLE_POINTS : Game.ROTTEN_APPLE_POINTS;
        }

        public override string ToData()
        {
            return movesLeft > Game.ROTTEN_APPLE_LIFESPAN ? "apple" : "apple-rot";
        }
    }

    public class GoldenApple(int x, int y) : Apple(x, y)
    {
        public override int Eat()
        {
            return movesLeft > Game.ROTTEN_APPLE_LIFESPAN ? Game.GOLDEN_APPLE_POINTS : Game.ROTTEN_GOLDEN_APPLE_POINTS;
        }

        public override string ToData()
        {
            return movesLeft > Game.ROTTEN_APPLE_LIFESPAN ? "golden-apple" : "golden-apple-rot";
        }
    }
}