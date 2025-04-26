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
        // POST api/wallet/create
        [HttpPost("create")]
        public async Task<IActionResult> CreateWallet([FromBody] Wallet wallet)
        {
            var userId = wallet.UserId;
            if (userId == null)
            return Unauthorized("User not found.");

            if (await _context.Wallets.AnyAsync(w => w.UserId == userId))
            return BadRequest("Wallet already exists for this user.");

            wallet.UserId = userId;
            _context.Wallets.Add(wallet);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetBalance), new { id = wallet.Id }, wallet);
        }

        // PUT api/wallet/update
        [HttpPut("update")]
        public async Task<IActionResult> UpdateWallet([FromBody] Wallet walletUpdate)
        {
            var userId = walletUpdate.UserId;
            if (userId == null)
            return Unauthorized("User not found.");

            var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
            if (wallet == null)
            {
            wallet = new Wallet
            {
                UserId = userId,
                Balance = walletUpdate.Balance
            };
            _context.Wallets.Add(wallet);
            }
            else
            {
            wallet.Balance = walletUpdate.Balance;
            _context.Wallets.Update(wallet);
            }

            await _context.SaveChangesAsync();
            return Ok(wallet);
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
