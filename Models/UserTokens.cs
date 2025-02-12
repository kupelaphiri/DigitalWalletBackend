namespace DigitalWalletAPI.Models
{
    public class UserToken
    {
        public int Id { get; set; }
        public int UserId { get; set; }  // User reference (foreign key)
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool Revoked { get; set; }
    }

}
