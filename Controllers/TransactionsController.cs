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
                RecipientEmail = transactionDto.RecipientEmail,
                Date = DateTime.UtcNow
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Transaction created successfully!" });
        }

        // GET api/transactions
       [HttpGet("{userId}")]
       public async Task<IActionResult> GetTransactions(int userId)
        {
        // Fetch all transactions for the given user ID
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
    public decimal Amount { get; set; }
    public string RecipientEmail { get; set; }
}
