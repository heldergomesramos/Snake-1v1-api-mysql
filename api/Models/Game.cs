using api.Dtos.Player;

namespace api.Models
{
    public class Game
    {
        public static readonly int SWAP_COOLDOWN = 7000;
        public static readonly int FREEZE_COOLDOWN = 12000;
        public static readonly int CUT_TAIL_COOLDOWN = 15000;

        public static readonly int FREEZE_TURNS = 5;
        public static readonly int QUICK_SAND_STUN_TURNS = 1;

        public static readonly int GOLDEN_APPLE_CHANCE = 10; /* Chance = 1 / GOLDEN_APPLE_CHANCE (1 / 10 = 0.1) */

        public static readonly int LAVA_POOL_DIVISOR = 25; /* N Pools = Size / LAVA_POOL_DIVISOR (100 / 25 = 4) */
        public static readonly int QUICK_SAND_DIVISOR = 60; /* N Pools = Size / QUICK_SAND_DIVISOR (100 / 60 = 1) */
        public static readonly int CACTUS_DIVISOR = 15; /* N Cactus = Size / CACTUS_DIVISOR (100 / 15 = 6) */

        private static readonly int tileVariations = 16;
        public string GameId { get; private set; } = string.Empty;
        public GenericLobby Lobby { get; private set; }
        public bool IsSinglePlayer { get; private set; } = false;

        public int Width { get { return Lobby?.GameSettings?.Width ?? 0; } }
        public int Height { get { return Lobby?.GameSettings?.Height ?? 0; } }
        public int Map { get { return Lobby?.GameSettings?.Map ?? 0; } }

        public int[][] GroundLayer { get; private set; }
        public IEntity?[][] EntityLayer { get; private set; }
        public IEntity?[][] SpecialGroundLayer { get; private set; }

        /* This should be in a separate ingame player class but oh well */
        public int Player1Score { get; private set; } = 0;
        public int Player2Score { get; private set; } = 0;
        public int Player1Cooldown { get; private set; } = 0;
        public int Player2Cooldown { get; private set; } = 0;
        public bool Player1WantsRematch { get; private set; } = false;
        public bool Player2WantsRematch { get; private set; } = false;

        public int GameTick { get; private set; } = 0;
        public int Time { get; private set; } = 3000;
        public int TickInterval { get; private set; } = 0;

        /* Sfx Events */
        private bool foodEatenThisTick = false;
        private bool swapThisTick = false;
        private bool freezeThisTick = false;
        private bool cutTailThisTick = false;

        public Dictionary<string, Snake> Snakes { get; private set; } = [];
        private Dictionary<string, Queue<char>> InputBuffer = [];
        public Apple? CurApple { get; private set; }
        public List<SnakeMeat> SnakeMeats { get; private set; } = [];
        public List<IEntity> Hazards { get; private set; } = [];

        public string[][] EntityLayerDataCopy { get; private set; }

        public enum GameState
        {
            Waiting,
            InProgress,
            Finished
        }
        public GameState GState { get; set; } = GameState.Waiting;

        public enum FinishedState
        {
            NotFinished,
            Player1Disconnected,
            Player2Disconnected,
            Player1WonByTimeOut,
            Player2WonByTimeOut,
            Player1WonByCollision,
            Player2WonByCollision,
            DrawByTimeOut,
            DrawByCollision,
            SinglePlayerTimeOut,
            SinglePlayerCollision
        }
        public FinishedState FState { get; set; } = FinishedState.NotFinished;

        public Game(GenericLobby lobby)
        {
            if (lobby == null || lobby.GameSettings == null)
            {
                throw new ArgumentException("Lobby and its GameSettings cannot be null.");
            }
            GameId = Guid.NewGuid().ToString();
            Lobby = lobby;
            IsSinglePlayer = Lobby.Player1 == null || Lobby.Player2 == null;
            var height = Lobby.GameSettings.Height;
            var width = Width;
            TickInterval = 1000 / Lobby.GameSettings.Speed;

            GroundLayer = new int[height][];
            for (int i = 0; i < height; i++)
            {
                GroundLayer[i] = new int[width];
                for (int j = 0; j < width; j++)
                    GroundLayer[i][j] = new Random().Next(tileVariations);
            }

            EntityLayer = new IEntity[height][];
            for (int i = 0; i < height; i++)
                EntityLayer[i] = new IEntity[width];

            SpecialGroundLayer = new IEntity[height][];
            for (int i = 0; i < height; i++)
                SpecialGroundLayer[i] = new IEntity[width];

            if (lobby.Player1 != null)
            {
                var playerId = lobby.Player1.PlayerId;
                Snakes[playerId] = new Snake(playerId, 1, Lobby.GameSettings.Height, Lobby.GameSettings.Width, this);
            }
            if (lobby.Player2 != null)
            {
                var playerId = lobby.Player2.PlayerId;
                Snakes[playerId] = new Snake(playerId, 2, Lobby.GameSettings.Height, Lobby.GameSettings.Width, this);
            }

            foreach (var sn in Snakes)
                AddSnakeToEntityLayer(sn.Value);

            if (Map == 1)
            {
                GenerateQuicksandPools();
                GenerateCactus();
            }
            else if (Map == 2)
                GenerateLavaPools();

            SpawnApple();

            EntityLayerDataCopy = new string[height][];
            for (int i = 0; i < height; i++)
                EntityLayerDataCopy[i] = new string[width];

            Player1Score = 0;
            Player2Score = 0;
            GameTick = 0;
        }

        public delegate Task GameTickHandler(Game game, bool foodEaten, bool swapThisTick, bool freezeThisTick, bool cutTailThisTick);
        public async Task StartGameLoop(GameTickHandler onTick)
        {
            Console.WriteLine("Start Game Loop");
            Time = 3000;
            while (Time > 0)
            {
                await Task.Delay(1000);
                if (GState == GameState.Finished)
                    break;
                Time -= 1000;
                try
                {
                    await onTick(this, false, false, false, false);
                }
                catch
                {
                    Console.WriteLine("Something went wrong");
                }
            }
            if (GState == GameState.Waiting)
            {
                GState = GameState.InProgress;
                Time = Lobby.GameSettings!.Time * 1000;
            }
            while (GState == GameState.InProgress)
            {
                await Task.Delay(TickInterval);
                Player1Cooldown -= TickInterval;
                Player2Cooldown -= TickInterval;
                UpdateGameState();
                await onTick(this, foodEatenThisTick, swapThisTick, freezeThisTick, cutTailThisTick);
                foodEatenThisTick = false;
                swapThisTick = false;
                freezeThisTick = false;
                cutTailThisTick = false;
            }
        }

        public static string GetOppositeDirection(string ogDirection)
        {
            return ogDirection switch
            {
                "l" => "r",
                "u" => "d",
                "r" => "l",
                "d" => "u",
                _ => " ",
            };
        }

        private PlayerSimplified? GetPlayerSimplifiedByPlayerId(string playerId)
        {
            if (Lobby.Player1 != null && Lobby.Player1.PlayerId == playerId)
                return Lobby.Player1;
            if (Lobby.Player2 != null && Lobby.Player2.PlayerId == playerId)
                return Lobby.Player2;
            return null;
        }

        private bool IsPlayer1(string playerId)
        {
            return Lobby.Player1 != null && Lobby.Player1.PlayerId == playerId;
        }

        public void UseAbility(string playerId)
        {
            if (GState != GameState.InProgress || !Lobby.GameSettings!.Abilities)
                return;
            var player = GetPlayerSimplifiedByPlayerId(playerId);
            if (player == null)
                return;
            if ((IsPlayer1(playerId) && Player1Cooldown > 0) || Player2Cooldown > 0)
                return;

            var playerSnake = Snakes[player.PlayerId];
            if (playerSnake.FrozenMoves > 0)
                return;

            switch (player.Ability)
            {
                //Swap
                case 0:
                    playerSnake.Swap(playerId);
                    InputBuffer[playerId] = new Queue<char>();
                    swapThisTick = true;
                    if (IsPlayer1(playerId))
                        Player1Cooldown = SWAP_COOLDOWN;
                    else
                        Player2Cooldown = SWAP_COOLDOWN;
                    break;
                //Freeze
                case 1:
                    freezeThisTick = true;
                    foreach (var snake in Snakes)
                        if (snake.Value.PlayerId != playerId)
                            snake.Value.Freeze();
                    if (IsPlayer1(playerId))
                        Player1Cooldown = FREEZE_COOLDOWN;
                    else
                        Player2Cooldown = FREEZE_COOLDOWN;
                    break;
                //Cut Tail
                case 2:
                    if (playerSnake.Segments.Count <= 1)
                        return;
                    cutTailThisTick = true;
                    SnakeMeats.Add(playerSnake.CutTail());
                    if (IsPlayer1(playerId))
                        Player1Cooldown = CUT_TAIL_COOLDOWN;
                    else
                        Player2Cooldown = CUT_TAIL_COOLDOWN;
                    break;
                default: return;
            }
        }

        public void WantsRematch(string playerId)
        {
            if (Lobby.Player1 != null && Lobby.Player1.PlayerId == playerId)
                Player1WantsRematch = !Player1WantsRematch;
            if (Lobby.Player2 != null && Lobby.Player2.PlayerId == playerId)
                Player2WantsRematch = !Player2WantsRematch;
        }

        public void SpawnApple()
        {
            Random random = new();
            int rows = EntityLayer.Length;
            int columns = EntityLayer[0].Length;

            int randomRow, randomColumn;
            do
            {
                randomRow = random.Next(0, rows);
                randomColumn = random.Next(0, columns);
            }
            while (EntityLayer[randomRow][randomColumn] != null);

            bool isGoldenApple = random.Next(0, GOLDEN_APPLE_CHANCE) == 0;

            var newApple = isGoldenApple ? new GoldenApple(randomColumn, randomRow) : new Apple(randomColumn, randomRow);
            EntityLayer[randomRow][randomColumn] = newApple;
            CurApple = newApple;
        }

        public void MoveSnake(Snake snake)
        {
            if (snake.HasSwapped)
            {
                snake.HasSwapped = false;
                return;
            }
            else if (snake.FrozenMoves > 0)
            {
                snake.FrozenMoves--;
                return;
            }
            else if (snake.QuicksandMoves > 0)
            {
                Console.WriteLine("Stuned for: " + snake.QuicksandMoves);
                snake.QuicksandMoves--;
                return;
            }

            char currentDirection = ProcessDirectionCommands(snake.PlayerId);
            if (GetOppositeDirection(currentDirection.ToString()) == snake.Head.Direction)
            {
                //Console.WriteLine("Prevented self collision bug");
                currentDirection = snake.Head.Direction[0];
            }

            int prevHeadX = snake.Head.X;
            int prevHeadY = snake.Head.Y;
            string afterHeadSegmentDirection = "";
            var gameSettings = Lobby.GameSettings;
            if (gameSettings == null)
            {
                Console.WriteLine("Game Settings is null");
                return;
            }

            switch (currentDirection)
            {
                case 'l':
                    snake.Head.X -= 1;
                    if (snake.Head.X < 0)
                    {
                        if (gameSettings.Borders)
                        {
                            snake.HasCollided = true;
                            return;
                        }
                        else
                        {
                            snake.Head.X = gameSettings.Width - 1;
                        }
                    }
                    if (snake.Head.Direction == "u")
                        afterHeadSegmentDirection = "ld";
                    else if (snake.Head.Direction == "d")
                        afterHeadSegmentDirection = "lu";
                    else
                        afterHeadSegmentDirection = "h";
                    snake.Head.Direction = "l";
                    break;
                case 'r':
                    snake.Head.X += 1;
                    if (snake.Head.X > gameSettings.Width - 1)
                    {
                        if (gameSettings.Borders)
                        {
                            snake.HasCollided = true;
                            return;
                        }
                        else
                        {
                            snake.Head.X = 0;
                        }
                    }
                    if (snake.Head.Direction == "u")
                        afterHeadSegmentDirection = "rd";
                    else if (snake.Head.Direction == "d")
                        afterHeadSegmentDirection = "ru";
                    else
                        afterHeadSegmentDirection = "h";
                    snake.Head.Direction = "r";
                    break;
                case 'u':
                    snake.Head.Y -= 1;
                    if (snake.Head.Y < 0)
                    {
                        if (gameSettings.Borders)
                        {
                            snake.HasCollided = true;
                            return;
                        }
                        else
                        {
                            snake.Head.Y = gameSettings.Height - 1;
                        }
                    }
                    if (snake.Head.Direction == "l")
                        afterHeadSegmentDirection = "ru";
                    else if (snake.Head.Direction == "r")
                        afterHeadSegmentDirection = "lu";
                    else
                        afterHeadSegmentDirection = "v";
                    snake.Head.Direction = "u";
                    break;
                case 'd':
                    snake.Head.Y += 1;
                    if (snake.Head.Y > gameSettings.Height - 1)
                    {
                        if (gameSettings.Borders)
                        {
                            snake.HasCollided = true;
                            return;
                        }
                        else
                        {
                            snake.Head.Y = 0;
                        }
                    }
                    if (snake.Head.Direction == "l")
                        afterHeadSegmentDirection = "rd";
                    else if (snake.Head.Direction == "r")
                        afterHeadSegmentDirection = "ld";
                    else
                        afterHeadSegmentDirection = "v";
                    snake.Head.Direction = "d";
                    break;
            }

            if (EntityLayer[snake.Head.Y][snake.Head.X] is Food food)
            {
                SnakeSegment newSegment = new(prevHeadX, prevHeadY, afterHeadSegmentDirection, "body", snake.PlayerNumber);
                snake.Segments.AddFirst(newSegment);
                if (snake.PlayerNumber == 1)
                    Player1Score += food.Eat();
                else
                    Player2Score += food.Eat();
                foodEatenThisTick = true;
                if (food is Apple)
                    SpawnApple();
                else if (food is SnakeMeat meat)
                    SnakeMeats.Remove(meat);
            }
            else
            {
                var lastSegment = snake.Segments.Last?.Value;
                if (lastSegment == null)
                    return;

                int prevLastSegmentX = lastSegment.X;
                int prevLastSegmentY = lastSegment.Y;
                string prevLastSegmentDirection = lastSegment.Direction;

                lastSegment.X = prevHeadX;
                lastSegment.Y = prevHeadY;
                lastSegment.Direction = afterHeadSegmentDirection;

                snake.Segments.RemoveLast();
                snake.Segments.AddFirst(lastSegment);

                // If apple, keep tail equal
                // Else update new tail position to be the previous last segment.
                snake.Tail.X = prevLastSegmentX;
                snake.Tail.Y = prevLastSegmentY;
                snake.Tail.Direction = Snake.GetTailDirection(prevLastSegmentDirection, snake.Tail.Direction);
            }
            if (SpecialGroundLayer[snake.Head.Y][snake.Head.X] is QuicksandPool)
                snake.QuicksandMoves = QUICK_SAND_STUN_TURNS;
        }

        private void AddSnakeToEntityLayer(Snake snake)
        {
            EntityLayer[snake.Head.Y][snake.Head.X] = snake.Head;
            foreach (IEntity segment in snake.Segments)
                EntityLayer[segment.Y][segment.X] = segment;
            EntityLayer[snake.Tail.Y][snake.Tail.X] = snake.Tail;
        }

        public void ReceiveDirectionCommand(string playerId, char command)
        {
            if (GState == GameState.Finished)
                return;

            if (!InputBuffer.ContainsKey(playerId))
                InputBuffer[playerId] = new Queue<char>();

            InputBuffer[playerId].Enqueue(command);
        }

        public char ProcessDirectionCommands(string playerId)
        {
            char curDirection = Snakes[playerId].Head.Direction[0];

            if (GState == GameState.Finished)
                return curDirection;

            if (!InputBuffer.TryGetValue(playerId, out Queue<char>? value))
            {
                InputBuffer[playerId] = new Queue<char>();
                return curDirection;
            }
            Queue<char> buffer = value;
            char validCommand = curDirection;

            while (buffer.Count > 0)
            {
                char command = buffer.Dequeue();

                if (!((curDirection == 'l' && command == 'r') ||
                      (curDirection == 'r' && command == 'l') ||
                      (curDirection == 'u' && command == 'd') ||
                      (curDirection == 'd' && command == 'u')))
                {
                    validCommand = command;
                    break;
                }
            }

            if (buffer.Count > 0)
            {
                var nextInQueue = buffer.Dequeue();
                buffer.Clear();
                buffer.Enqueue(nextInQueue);
            }
            return validCommand;
        }

        public void HandleDisconnection(string playerId)
        {
            if (GState == GameState.Finished)
                return;
            if (Lobby.Player1 != null && Lobby.Player1.PlayerId == playerId)
                EndGame(FinishedState.Player1Disconnected);
            else if (Lobby.Player2 != null && Lobby.Player2.PlayerId == playerId)
                EndGame(FinishedState.Player2Disconnected);
            else
                Console.WriteLine("[ERROR] Lobby.Player is null or wrong playerId");
        }

        private void EndGame(FinishedState newState)
        {
            Console.WriteLine("End Game on State: " + newState.ToString());
            GState = GameState.Finished;
            FState = newState;
            Lobby.GameStarted = false;

            if (IsSinglePlayer)
                return;

            var player1 = Lobby.Player1!;
            var player2 = Lobby.Player2!;

            switch (newState)
            {
                case FinishedState.Player1Disconnected:
                    player1.Losses++;
                    player2.Wins++;
                    break;

                case FinishedState.Player2Disconnected:
                    player1.Wins++;
                    player2.Losses++;
                    break;

                case FinishedState.Player1WonByTimeOut:
                case FinishedState.Player1WonByCollision:
                    player1.Wins++;
                    player2.Losses++;
                    break;

                case FinishedState.Player2WonByTimeOut:
                case FinishedState.Player2WonByCollision:
                    player1.Losses++;
                    player2.Wins++;
                    break;
            }
        }

        public Snake? GetPlayerSnake(int playerNumber)
        {
            Snake? player1Snake = null, player2Snake = null;

            if (Lobby.Player1 != null)
                player1Snake = Snakes.Values.FirstOrDefault(s => s.PlayerId == Lobby.Player1.PlayerId);

            if (Lobby.Player2 != null)
                player2Snake = Snakes.Values.FirstOrDefault(s => s.PlayerId == Lobby.Player2.PlayerId);

            return playerNumber == 1 ? player1Snake : player2Snake;
        }

        private void EndGameIfCollisionDetected()
        {
            if (IsSinglePlayer || Lobby.Player1 == null || Lobby.Player2 == null || Snakes.Count != 2)
                return;

            var snake1 = Snakes.First().Value;
            var snake2 = Snakes.Last().Value;

            /* End Game if at least 1 player lost */
            if (snake1.HasCollided && snake2.HasCollided)
                EndGame(FinishedState.DrawByCollision);
            else if (snake1.HasCollided)
                EndGame(FinishedState.Player2WonByCollision);
            else if (snake2.HasCollided)
                EndGame(FinishedState.Player1WonByCollision);
        }

        public void UpdateGameState()
        {
            Time -= TickInterval;
            GameTick++;
            if (Time <= 0)
            {
                if (IsSinglePlayer)
                    EndGame(FinishedState.SinglePlayerTimeOut);
                else if (Player1Score > Player2Score)
                    EndGame(FinishedState.Player1WonByTimeOut);
                else if (Player1Score < Player2Score)
                    EndGame(FinishedState.Player2WonByTimeOut);
                else
                    EndGame(FinishedState.DrawByTimeOut);
            }

            foreach (var sn in Snakes)
                MoveSnake(sn.Value);

            DetectSinglePlayerCollisions();
            DetectMultiPlayerCollisions();
            EndGameIfCollisionDetected();

            if (CurApple != null)
            {
                CurApple.MovesLeft--;
                if (CurApple.MovesLeft <= 0)
                    SpawnApple();
            }

            foreach (var meat in SnakeMeats)
                meat.MovesLeft--;

            SnakeMeats.RemoveAll(meat => meat.MovesLeft <= 0);

            if (GState == GameState.Finished)
            {
                Console.WriteLine("Game Ended in collision");
                return;
            }

            UpdateEntityLayer();
        }

        private void UpdateEntityLayer()
        {
            if (EntityLayer == null || EntityLayer[0] == null)
                return;

            for (int i = 0; i < EntityLayer.Length; i++)
                for (int j = 0; j < EntityLayer[i].Length; j++)
                    EntityLayer[i][j] = null;

            foreach (var sn in Snakes)
                AddSnakeToEntityLayer(sn.Value);

            foreach (var meat in SnakeMeats)
                EntityLayer[meat.Y][meat.X] = meat;

            foreach (var haz in Hazards)
                EntityLayer[haz.Y][haz.X] = haz;

            if (CurApple != null)
                EntityLayer[CurApple.Y][CurApple.X] = CurApple;
        }

        private void DetectSinglePlayerCollisions()
        {
            if (!IsSinglePlayer)
                return;

            Snake snake = Snakes.Values.First();
            if (snake.HasCollided)
            {
                EndGame(FinishedState.SinglePlayerCollision);
                return;
            }

            foreach (var segment in snake.Segments)
            {
                if (snake.Head.X == segment.X && snake.Head.Y == segment.Y)
                {
                    snake.HasCollided = true;
                    break;
                }
            }
            if (snake.Head.X == snake.Tail.X && snake.Head.Y == snake.Tail.Y)
                snake.HasCollided = true;

            if (EntityLayer[snake.Head.Y][snake.Head.X] is IObstacle)
                snake.HasCollided = true;

            if (snake.HasCollided)
                EndGame(FinishedState.SinglePlayerCollision);
        }

        private void DetectMultiPlayerCollisions()
        {
            if (IsSinglePlayer)
                return;

            foreach (var snakeEntry in Snakes)
            {
                var snake = snakeEntry.Value;
                /* If border collision has been marked previously on MoveSnake() */
                if (snake.HasCollided)
                    continue;

                if (EntityLayer[snake.Head.Y][snake.Head.X] is IObstacle)
                {
                    snake.HasCollided = true;
                    continue;
                }

                var snakeHead = snake.Head;
                foreach (var snakeBodyEntry in Snakes)
                {
                    var snakeBody = snakeBodyEntry.Value.Segments;
                    foreach (var segment in snakeBody)
                        if (snakeHead.X == segment.X && snakeHead.Y == segment.Y)
                        {
                            snake.HasCollided = true;
                            break;
                        }
                    var snakeTail = snakeBodyEntry.Value.Tail;
                    if (snakeHead.X == snakeTail.X && snakeHead.Y == snakeTail.Y)
                        snake.HasCollided = true;
                }
            }

            /* Heads Collision */
            if (Snakes.Count != 2)
                return;

            var snake1 = Snakes.First().Value;
            var snake2 = Snakes.Last().Value;
            if (snake1.Head.X == snake2.Head.X && snake1.Head.Y == snake2.Head.Y)
            {
                snake1.HasCollided = true;
                snake2.HasCollided = true;
            }
        }

        public GameData ToResponseDto()
        {
            var newData = new GameData(this);
            EntityLayerDataCopy = newData.EntityLayer;
            return newData;
        }

        /* Hazards */

        public void GenerateCactus()
        {
            Random rand = new();

            int baseNumberOfCactus = Width * Height / CACTUS_DIVISOR;
            int adjustment = rand.Next(0, 5) - 2;
            int numberOfCactus = baseNumberOfCactus + adjustment;

            for (int i = 0; i < numberOfCactus; i++)
            {
                bool isPlaced = false;

                while (!isPlaced)
                {
                    int startX = rand.Next(0, Width);
                    int startY = rand.Next(0, Height);
                    int cactusVariant = rand.Next(0, 3);

                    if (EntityLayer[startY][startX] == null && SpecialGroundLayer[startY][startX] == null)
                    {
                        var cactus = new Cactus(startX, startY, cactusVariant);
                        EntityLayer[startY][startX] = cactus;
                        Hazards.Add(cactus);
                        isPlaced = true;
                    }
                }
            }
        }

        public void GenerateLavaPools()
        {
            Random rand = new();

            int baseNumberOfPools = Width * Height / LAVA_POOL_DIVISOR;
            int adjustment = rand.Next(0, 3) - 1; // Generates -1, 0, or +1
            int numberOfPools = baseNumberOfPools + adjustment;

            for (int i = 0; i < numberOfPools; i++)
            {
                bool isPlaced = false;

                while (!isPlaced)
                {
                    int startX = rand.Next(0, Width);
                    int startY = rand.Next(0, Height);
                    int poolType = rand.Next(0, 4);

                    List<(int x, int y)> poolCoords = GetLavaPoolCoordinates(startX, startY, poolType);

                    if (CanPlacePool(poolCoords))
                    {
                        PlaceLavaPool(poolCoords, poolType);
                        isPlaced = true;
                    }
                }
            }
        }

        public void GenerateQuicksandPools()
        {
            Random rand = new();

            int baseNumberOfPools = Width * Height / QUICK_SAND_DIVISOR;
            int adjustment = rand.Next(0, 3) - 1;
            int numberOfPools = baseNumberOfPools + adjustment;

            for (int i = 0; i < numberOfPools; i++)
            {
                bool isPlaced = false;

                while (!isPlaced)
                {
                    int startX = rand.Next(0, Width);
                    int startY = rand.Next(0, Height);
                    int poolType = rand.Next(0, 2); // 0 for vertical, 1 for horizontal

                    List<(int x, int y)> poolCoords = GetQuicksandPoolCoordinates(startX, startY, poolType);

                    if (CanPlacePool(poolCoords))
                    {
                        PlaceQuicksandPool(poolCoords, poolType);
                        isPlaced = true;
                    }
                }
            }
        }

        private static List<(int x, int y)> GetLavaPoolCoordinates(int startX, int startY, int poolType)
        {
            return poolType switch
            {
                0 => [(startX, startY), (startX, startY + 1), (startX, startY + 2), (startX, startY + 3)],
                1 => [(startX, startY), (startX + 1, startY), (startX + 2, startY)],
                2 => [(startX, startY), (startX + 1, startY), (startX, startY + 1), (startX + 1, startY + 1)],
                3 => [(startX, startY)],
                _ => [],
            };
        }

        private static List<(int x, int y)> GetQuicksandPoolCoordinates(int startX, int startY, int poolType)
        {
            return poolType switch
            {
                0 => [(startX, startY), (startX + 1, startY), (startX, startY + 1), (startX + 1, startY + 1),
              (startX, startY + 2), (startX + 1, startY + 2), (startX, startY + 3), (startX + 1, startY + 3)], // Vertical 4x2
                1 => [(startX, startY), (startX + 1, startY), (startX + 2, startY), (startX + 3, startY),
              (startX, startY + 1), (startX + 1, startY + 1), (startX + 2, startY + 1), (startX + 3, startY + 1)], // Horizontal 2x4
                _ => []
            };
        }

        private bool CanPlacePool(List<(int x, int y)> poolCoords)
        {
            foreach (var (x, y) in poolCoords)
                if (x < 0 || y < 0 || x >= Width || y >= Height || EntityLayer[y][x] != null || SpecialGroundLayer[y][x] != null)
                    return false;
            return true;
        }

        private void PlaceLavaPool(List<(int x, int y)> poolCoords, int poolType)
        {
            for (int i = 0; i < poolCoords.Count; i++)
            {
                var (x, y) = poolCoords[i];
                var pool = new LavaPool(x, y, poolType, i);
                Hazards.Add(pool);
                EntityLayer[y][x] = pool;
            }
        }

        private void PlaceQuicksandPool(List<(int x, int y)> poolCoords, int poolType)
        {
            for (int i = 0; i < poolCoords.Count; i++)
            {
                var (x, y) = poolCoords[i];
                var pool = new QuicksandPool(x, y, poolType, i);
                SpecialGroundLayer[y][x] = pool;
            }
        }
    }
}