using api.Services;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [Route("api/app")]
    [ApiController]
    public class AppController(ILogger<AppController> logger, IPlayerService playerService) : ControllerBase
    {
        private readonly ILogger<AppController> _logger = logger;
        private readonly IPlayerService _playerService = playerService;

        [HttpGet("ping")]
        public async Task<IActionResult> Ping()
        {
            _logger.LogInformation("Ping!");
            try
            {
                await _playerService.PingDatabaseAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting to the database.");
                return StatusCode(500, new { message = "Database connection failed." });
            }
            return Ok(new { message = "Ping successful" });
        }
    }
}
