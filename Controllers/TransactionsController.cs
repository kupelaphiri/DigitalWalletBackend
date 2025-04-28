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
    // Start a database transaction
    using var dbTransaction = await _context.Database.BeginTransactionAsync();
    
    try
    {
        var userId = transactionDto.UserId;
        
        // Get the wallet (including the row lock for update)
        var wallet = await _context.Wallets
            .Where(w => w.UserId == userId)
            .FirstOrDefaultAsync();
            
        if (wallet == null)
            return BadRequest("Wallet not found.");
            
        if (wallet.Balance < transactionDto.Amount)
            return BadRequest("Insufficient balance.");

        // Update the wallet balance
        wallet.Balance -= transactionDto.Amount;

        // Create a new transaction
        var transaction = new Transaction
        {
            UserId = userId, 
            Amount = transactionDto.Amount,
            RecipientEmail = transactionDto.RecipientEmail,
            Date = DateTime.UtcNow
        };

        var expense = new Expense
        {
            UserId = userId,
            Amount = transactionDto.Amount,
            Title = "Transaction",
            Date = DateTime.UtcNow,
            Category = "Transfer"
        };

        _context.Transactions.Add(transaction);
        _context.Expenses.Add(expense);
        
        // Save all changes
        await _context.SaveChangesAsync();
        
        // Commit the transaction if all operations succeeded
        await dbTransaction.CommitAsync();

        return Ok(new { message = "Transaction created successfully!" });
    }
    catch (Exception ex)
    {
        // Roll back if any operation fails
        await dbTransaction.RollbackAsync();
        return StatusCode(500, "An error occurred while processing the transaction.");
    }
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
    public int UserId { get; set; }
}
