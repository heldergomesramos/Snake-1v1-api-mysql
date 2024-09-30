using api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace api.Controllers
{
    [Route("api/app")]
    [ApiController]
    public class AppController : ControllerBase
    {
        private readonly ApplicationDBContext _context;
        private readonly ILogger<AppController> _logger;

        // Inject the ApplicationDBContext and ILogger<AppController>
        public AppController(ApplicationDBContext context, ILogger<AppController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("ping")]
        public async Task<IActionResult> Ping()
        {
            try
            {
                _logger.LogInformation("Ping request received.");

                // Fetch players from the database
                //var players = await _context.Players.ToListAsync();

                // Log the count of players retrieved
                //_logger.LogInformation($"Number of players retrieved: {players.Count}");

                return Ok(new { message = "Ping successful" });
            }
            catch (Exception ex)
            {
                // Log the exception
                _logger.LogError(ex, "An error occurred while processing the ping request.");
                return StatusCode(500, new { message = "Internal server error xD" });
            }
        }
    }
}
