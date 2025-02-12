using Microsoft.AspNetCore.Mvc;
using DigitalWalletAPI.Data;
using DigitalWalletAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using System.IdentityModel.Tokens.Jwt;

namespace DigitalWalletAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TransactionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TransactionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // POST api/transactions
        [HttpPost]
        public async Task<IActionResult> CreateTransaction([FromBody] TransactionCreateDto transactionDto)
        {
            // Log the claims in the token for debugging
            var claims = User.Claims.ToList();
            foreach (var claim in claims)
            {
                Console.WriteLine($"Claim Type: {claim.Type}, Claim Value: {claim.Value}");
            }

            var userIdString = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            Console.WriteLine($"Extracted UserId (sub): {userIdString}");

            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                return Unauthorized("User ID is missing or invalid.");
            }

            // Create a new transaction
            var transaction = new Transaction
            {
                UserId = userId,  // Safely set the userId from the JWT token
                Amount = transactionDto.Amount,
                RecipientUsername = transactionDto.RecipientUsername,
                Date = DateTime.UtcNow
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Transaction created successfully!" });
        }

        // GET api/transactions
        [HttpGet]
        public async Task<IActionResult> GetTransactions()
        {
            // Get the logged-in user's ID from the JWT token
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                return Unauthorized("User ID is invalid.");
            }

            // Fetch all transactions for the logged-in user
            var transactions = await _context.Transactions
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.Date)
                .ToListAsync();

            return Ok(transactions);
        }
    }
}

public class TransactionCreateDto
{
    public decimal Amount { get; set; } // Transaction amount
    public string RecipientUsername { get; set; } // Username of the recipient
}
