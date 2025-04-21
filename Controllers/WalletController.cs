using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;
using DigitalWalletAPI.Data;
using DigitalWalletAPI.Models;

namespace DigitalWalletAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class WalletController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public WalletController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET api/wallet/balance
        [HttpGet("balance")]
        public async Task<IActionResult> GetBalance()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized("User not found.");

            var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId.ToString() == userId);
            if (wallet == null)
                return NotFound("Wallet not found for this user.");

            return Ok(new { balance = wallet.Balance });
        }
    }
}
