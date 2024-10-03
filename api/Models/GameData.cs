using api.Dtos.Lobby;
using api.Mappers;
using static api.Models.Game;

namespace api.Models
{
    public class GameData
    {
        public string GameId { get; private set; } = string.Empty;
        public LobbyResponseDto Lobby { get; private set; }

        public int[][] GroundLayer { get; private set; }
        public string[][] EntityLayer { get; private set; }

        public int Player1Score { get; private set; } = 0;
        public int Player2Score { get; private set; } = 0;
        public int Player1Cooldown { get; private set; } = 0;
        public int Player2Cooldown { get; private set; } = 0;
        public bool Player1Frozen { get; private set; } = false;
        public bool Player2Frozen { get; private set; } = false;
        public int GameTick { get; private set; } = 0;
        public int Time { get; private set; } = 3;

        public bool IsSinglePlayer { get; private set; }

        public string FinishedState { get; private set; } = Game.FinishedState.NotFinished.ToString();

        public GameData(Game game)
        {
            GameId = game.GameId;
            Lobby = LobbyMappers.ToResponseDto(game.Lobby);
            GroundLayer = game.GroundLayer;
            EntityLayer = game.GState == GameState.Finished ? game.EntityLayerDataCopy : EntityLayerToData(game.EntityLayer);
            Player1Score = game.Player1Score;
            Player2Score = game.Player2Score;
            Player1Cooldown = (int)Math.Ceiling(game.Player1Cooldown / 1000.0);
            Player2Cooldown = (int)Math.Ceiling(game.Player2Cooldown / 1000.0);
            var snake1 = game.GetPlayerSnake(1);
            var snake2 = game.GetPlayerSnake(2);
            Player1Frozen = snake1 != null && snake1.FrozenMoves > 0;
            Player2Frozen = snake2 != null && snake2.FrozenMoves > 0;
            GameTick = game.GameTick;
            Time = game.Time / 1000;
            FinishedState = game.FState.ToString();
            IsSinglePlayer = game.IsSinglePlayer;
        }

        private static string[][] EntityLayerToData(IEntity?[][] layer)
        {
            int height = layer.Length;
            int width = layer[0].Length;

            string[][] dataLayer = new string[height][];

            for (int i = 0; i < height; i++)
            {
                dataLayer[i] = new string[width];
                for (int j = 0; j < width; j++)
                {
                    if (layer[i][j] != null)
                        dataLayer[i][j] = layer[i][j]!.ToData();
                    else
                        dataLayer[i][j] = "empty";
                }
            }
            return dataLayer;
        }
    }
}