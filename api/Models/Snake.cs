namespace api.Models
{
    public class Snake
    {
        public string PlayerId { get; set; } = string.Empty;
        public int PlayerNumber;
        public LinkedList<SnakeSegment> Segments { get; set; } = [];
        public SnakeSegment Head { get; set; }
        public SnakeSegment Tail { get; set; }
        public bool HasCollided { get; set; } = false;
        public bool HasSwapped { get; set; } = false;
        public int FrozenMoves { get; set; } = 0;
        public int QuicksandMoves { get; set; } = 0;
        public Game Game { get; set; }

        public Snake(string playerId, int playerNumber, int gameHeight, int gameWidth, Game game)
        {
            Game = game;
            PlayerId = playerId;
            PlayerNumber = playerNumber;
            int y;
            int tailX, bodyX, headX;

            if (gameHeight % 2 == 0)
            {
                // If the board height is even, snakes are 1 vertical tile apart
                y = playerNumber == 1 ? (gameHeight / 2) - 1 : (gameHeight / 2);
            }
            else
            {
                // If the board height is odd, snakes are 2 vertical tiles apart
                y = playerNumber == 1 ? (gameHeight / 2) - 1 : (gameHeight / 2) + 1;
            }

            if (playerNumber == 1)
            {
                // Snake 1 faces right
                tailX = 1;
                bodyX = 2;
                headX = 3;
            }
            else
            {
                // Snake 2 faces left
                tailX = gameWidth - 2;
                bodyX = gameWidth - 3;
                headX = gameWidth - 4;
            }

            Tail = new SnakeSegment(tailX, y, playerNumber == 1 ? "r" : "l", "tail", playerNumber);
            var body = new SnakeSegment(bodyX, y, "h", "body", playerNumber);
            Head = new SnakeSegment(headX, y, playerNumber == 1 ? "r" : "l", "head", playerNumber);

            Segments.AddLast(body);
        }

        public void Swap(string playerId)
        {
            var headX = Head.X;
            var headY = Head.Y;
            var headDir = Head.Direction;

            Head.X = Tail.X;
            Head.Y = Tail.Y;
            Head.Direction = Game.GetOppositeDirection(Tail.Direction);

            Tail.X = headX;
            Tail.Y = headY;
            Tail.Direction = Game.GetOppositeDirection(headDir);

            if (Segments.Count > 1)
            {
                var reversedSegments = new LinkedList<SnakeSegment>(Segments);
                Segments.Clear();
                foreach (var segment in reversedSegments.Reverse())
                    Segments.AddLast(segment);
            }

            HasSwapped = true;
        }

        public void Freeze()
        {
            FrozenMoves = Game.FREEZE_TURNS;
        }

        public SnakeMeat CutTail()
        {
            SnakeMeat meat = new(Tail.X, Tail.Y);
            var lastSegment = Segments.Last();
            Segments.Remove(lastSegment);
            Tail.X = lastSegment.X;
            Tail.Y = lastSegment.Y;
            Tail.Direction = GetTailDirection(lastSegment.Direction, Tail.Direction);
            return meat;
        }

        public static string GetTailDirection(string prevLastSegmentDirection, string tailDirection)
        {
            if ((prevLastSegmentDirection == "ru" || prevLastSegmentDirection == "lu") && (tailDirection == "l" || tailDirection == "r"))
                return "u";
            else if (prevLastSegmentDirection == "ru" && tailDirection == "d")
                return "r";
            else if (prevLastSegmentDirection == "lu" && tailDirection == "d")
                return "l";
            else if ((prevLastSegmentDirection == "rd" || prevLastSegmentDirection == "ld") && (tailDirection == "l" || tailDirection == "r"))
                return "d";
            else if (prevLastSegmentDirection == "rd" && tailDirection == "u")
                return "r";
            else if (prevLastSegmentDirection == "ld" && tailDirection == "u")
                return "l";
            return tailDirection;
        }
    }
}