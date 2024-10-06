using api.Data;
using api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace api.Controllers
{
    [Route("api/app")]
    [ApiController]
    public class AppController : ControllerBase
    {
        private readonly ILogger<AppController> _logger;
        private readonly IPlayerService _playerService;

        public AppController(ILogger<AppController> logger, IPlayerService playerService)
        {
            _logger = logger;
            _playerService = playerService;
        }

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
