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

    public abstract class Food(int x, int y) : IEntity(x, y)
    {
        public int MovesLeft { get; set; } = 35;
        public abstract int NormalScore { get; set; }
        public abstract int RottenScore { get; set; }
        public int RottenLifespan { get; set; } = 5;
        public abstract string NormalData { get; set; }
        public abstract string RottenData { get; set; }

        public int Eat()
        {
            return MovesLeft > RottenLifespan ? NormalScore : RottenScore;
        }

        public override string ToData()
        {
            return MovesLeft > RottenLifespan ? NormalData : RottenData;
        }
    }

    public class Apple(int x, int y) : Food(x, y)
    {
        public override int NormalScore { get; set; } = 100;
        public override int RottenScore { get; set; } = 50;
        public override string NormalData { get; set; } = "apple";
        public override string RottenData { get; set; } = "apple-rot";
    }

    public class GoldenApple(int x, int y) : Apple(x, y)
    {
        public override int NormalScore { get; set; } = 300;
        public override int RottenScore { get; set; } = 50;
        public override string NormalData { get; set; } = "golden-apple";
        public override string RottenData { get; set; } = "golden-apple-rot";
    }

    public class SnakeMeat(int x, int y) : Food(x, y)
    {
        public override int NormalScore { get; set; } = 100;
        public override int RottenScore { get; set; } = 50;
        public override string NormalData { get; set; } = "snake-meat";
        public override string RottenData { get; set; } = "snake-meat-rot";
    }

    public abstract class Obstacle(int x, int y) : IEntity(x, y) { }

    public class LavaPool : Obstacle
    {
        public int PoolType { get; set; }
        public int SegmentIndex { get; set; }

        public LavaPool(int x, int y, int poolType, int segmentIndex) : base(x, y)
        {
            PoolType = poolType;
            SegmentIndex = segmentIndex;
        }

        public override string ToData()
        {
            return PoolType switch
            {
                0 => $"lava-vertical-{SegmentIndex}",
                1 => $"lava-horizontal-{SegmentIndex}",
                2 => $"lava-circle-large-{SegmentIndex}",
                3 => "lava-circle-small",
                _ => "unknown",
            };
        }
    }

}