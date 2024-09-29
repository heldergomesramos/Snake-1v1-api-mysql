using api.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace api.Controllers
{
    [Route("api/app")]
    [ApiController]
    public class AppController(ApplicationDBContext context) : ControllerBase
    {
        private readonly ApplicationDBContext _context = context;

        [HttpGet("ping")]
        public async Task<IActionResult> Ping()
        {
            _ = await _context.Players.ToListAsync();
            return Ok(new { message = "Ping successful" });
        }

        [HttpGet("version")]
        public async Task<IActionResult> GetMySQLVersion()
        {
            var version = await _context.Database.ExecuteSqlRawAsync("SELECT VERSION();");
            return Ok(new { version });
        }
    }
}