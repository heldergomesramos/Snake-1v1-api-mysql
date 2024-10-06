using api.Dtos.Player;
using api.Models;

namespace api.Services
{
    public interface IPlayerService
    {
        Task PingDatabaseAsync();
        Task UpdatePlayerAsync(PlayerSimplified player);
        Task<PlayerSimplified?> GetPlayerSimplifiedByIdAsync(string id);
        Task<List<PlayerSimplified>> GetAllPlayersSimplifiedAsync();
        Task<PlayerRegisterResponseDto?> RegisterPlayerAsync(PlayerRegisterRequestDto dto);
        Task<PlayerRegisterResponseDto?> LoginPlayerAsync(PlayerRegisterRequestDto dto);
        PlayerRegisterResponseDto? CreateGuest();
    }
}
