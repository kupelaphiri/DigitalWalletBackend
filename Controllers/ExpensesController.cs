using Microsoft.AspNetCore.Mvc;
using DigitalWalletAPI.Models;
using DigitalWalletAPI.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Any;


namespace DigitalWalletAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExpensesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ExpensesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/expenses
       [HttpGet("{userId}")]
        public async Task<ActionResult<IEnumerable<Expense>>> GetExpenses(int userId)
        {
         if (userId == null)
         return BadRequest("UserId is required.");

         var expenses = await _context.Expenses
            .Where(e => e.UserId == userId)
            .ToListAsync();

          return Ok(expenses);
        }

        // POST: api/expenses
         [HttpPost]
        public async Task<ActionResult<Expense>> AddExpense([FromBody] Expense expense)
        {
            Console.WriteLine($"Expense: {expense.Title}, {expense.Amount}, {expense.Date}, {expense.Category}, {expense.UserId}");
            var userId = expense.UserId;
            var wallet = await _context.Wallets
            .Where(w => w.UserId == userId)
            .FirstOrDefaultAsync();
            
             if (wallet == null)
               return BadRequest("Wallet not found.");
            
             if (wallet.Balance < expense.Amount)
               return BadRequest("Insufficient balance.");

             // Update the wallet balance
             wallet.Balance -= expense.Amount;
             expense.Date = DateTime.SpecifyKind(expense.Date, DateTimeKind.Utc);
            _context.Expenses.Add(expense);
            await _context.SaveChangesAsync();

            var response = new
            {
            Expense = expense,
            NewBalance = wallet.Balance
            };

            return CreatedAtAction(nameof(GetExpenseById), new { id = expense.Id }, response);
        }

        // GET: api/expenses/{id}
        [HttpGet("/get-expense/{id}")]
        public async Task<ActionResult<Expense>> GetExpenseById(int id)
        {
            var expense = await _context.Expenses.FindAsync(id);

            if (expense == null)
                return NotFound();

            return expense;
        }

        // PUT: api/expenses/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateExpense(int id, [FromBody] Expense updatedExpense)
        {
            if (id != updatedExpense.Id)
                return BadRequest("ID mismatch");

            var existingExpense = await _context.Expenses.FindAsync(id);
            if (existingExpense == null)
                return NotFound();

            existingExpense.Title = updatedExpense.Title;
            existingExpense.Amount = updatedExpense.Amount;
            existingExpense.Date = updatedExpense.Date;
            existingExpense.Category = updatedExpense.Category;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/expenses/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteExpense(int id)
        {
            var expense = await _context.Expenses.FindAsync(id);
            if (expense == null)
                return NotFound();

            _context.Expenses.Remove(expense);
            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}
