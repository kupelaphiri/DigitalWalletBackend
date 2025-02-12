using Microsoft.EntityFrameworkCore;
using DigitalWalletAPI.Models;


namespace DigitalWalletAPI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; } // Add your database tables here
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<UserToken> UserTokens { get; set; }
    }
}
