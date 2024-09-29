using api.Models;

namespace api.Dtos.Player
{
    public class PlayerSimplified
    {
        public string PlayerId { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public bool IsGuest { get; set; } = false;
        public int Wins { get; set; } = 0;
        public int Losses { get; set; } = 0;
        public int Color { get; set; } = 0;
        public int Ability { get; set; } = 0;
        public GenericLobby? Lobby { get; set; } = null;
        public Game? Game { get; set; } = null;

        public void UpdateColor(int color)
        {
            Color = Math.Clamp(color, 0, 7);
        }

        public void UpdateAbility(int ability)
        {
            Ability = Math.Clamp(ability, 0, 2);
        }
    }
}