namespace DigitalWalletAPI.Models
{
    public class Transaction
    {
        public int Id { get; set; }
        public int UserId { get; set; } 
        public decimal Amount { get; set; } 
        public string RecipientEmail { get; set; } 
        public DateTime Date { get; set; } 
    }
}
