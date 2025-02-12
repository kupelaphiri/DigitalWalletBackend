namespace DigitalWalletAPI.Models
{
    public class Transaction
    {
        public int Id { get; set; } // Unique identifier for the transaction
        public int UserId { get; set; } // User ID who initiated the transaction
        public decimal Amount { get; set; } // Transaction amount
        public string RecipientUsername { get; set; } // Username of the recipient
        public DateTime Date { get; set; } // Date of the transaction
    }
}
