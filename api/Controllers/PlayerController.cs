using api.Dtos.Player;
using api.Services;
using api.Managers;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [Route("api/player")]
    [ApiController]
    public class PlayerController(IPlayerService playerService, ILogger<PlayerController> logger) : ControllerBase
    {
        private readonly IPlayerService _playerService = playerService;
        private readonly ILogger<PlayerController> _logger = logger;

        /* Commented Methods are the ones not used in practice */

        // [HttpGet("all")]
        // public async Task<IActionResult> GetAllPlayers()
        // {
        //     var players = await _playerService.GetAllPlayersSimplifiedAsync();
        //     return Ok(players);
        // }

        // [HttpGet("all-connected")]
        // public IActionResult GetAllConnectedPlayers()
        // {
        //     var players = PlayerManager.GetAllConnectedPlayers();
        //     return Ok(players);
        // }

        // [HttpGet("details/{id}")]
        // public async Task<IActionResult> GetPlayerDetails(string id)
        // {
        //     var player = await _playerService.GetPlayerSimplifiedByIdAsync(id);

        //     if (player == null)
        //         return NotFound("Player not found.");

        //     return Ok(player);
        // }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] PlayerRegisterRequestDto dto)
        {
            _logger.LogInformation("Register from: " + dto.Username);
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest();
                if (dto.Username.Length > 12)
                    return StatusCode(400, new { message = "Username cannot be longer than 12 characters long." });
                if (dto.Username.StartsWith("Guest"))
                    return StatusCode(400, new { message = "Username cannot start with \"Guest\"." });

                var responseDto = await _playerService.RegisterPlayerAsync(dto);
                if (responseDto == null)
                    return StatusCode(409, new { message = "Username already exists." });
                return Ok(responseDto);
            }
            catch (Exception e)
            {
                return StatusCode(500, new { message = e });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] PlayerRegisterRequestDto dto)
        {
            _logger.LogInformation("Login from: " + dto.Username);
            if (!ModelState.IsValid)
                return BadRequest();

            var response = await _playerService.LoginPlayerAsync(dto);

            if (response == null)
                return Unauthorized(new { message = "Invalid username or password." });

            if (PlayerManager.IsPlayerConnected(response.PlayerId))
                return StatusCode(403, new { message = "Player is alredy connected." });
            return Ok(response);
        }

        [HttpPost("guest")]
        public IActionResult Guest()
        {
            _logger.LogInformation("Guest joined");
            var guestPlayerDto = _playerService.CreateGuest();
            if (guestPlayerDto == null)
                return StatusCode(500, new { message = "Failed to create guest player." });
            return Ok(new { status = "guest_joined", player = guestPlayerDto });
        }
    }
}