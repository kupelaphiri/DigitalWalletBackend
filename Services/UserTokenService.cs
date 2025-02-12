using DigitalWalletAPI.Data;
using DigitalWalletAPI.Models;
using Microsoft.EntityFrameworkCore;


namespace DigitalWalletAPI.Services
{
    public class UserTokenService
    {
        private readonly ApplicationDbContext _context;

        public UserTokenService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Save tokens to the database
        public async Task SaveTokensAsync(int userId, string accessToken, string refreshToken)
        {
            var existingTokens = await _context.UserTokens.Where(t => t.UserId == userId && !t.Revoked).ToListAsync();

            // If tokens exist, revoke them first
            foreach (var token in existingTokens)
            {
                token.Revoked = true;
            }

            // Create new token entry
            var userToken = new UserToken
            {
                UserId = userId,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7), // Set the expiry of the refresh token
                Revoked = false
            };

            await _context.UserTokens.AddAsync(userToken);
            await _context.SaveChangesAsync();
        }

        // Retrieve refresh token from the database
        public async Task<UserToken> GetUserTokenAsync(string refreshToken)
        {
            return await _context.UserTokens
                .Where(t => t.RefreshToken == refreshToken && !t.Revoked)
                .FirstOrDefaultAsync();
        }

        // Delete tokens from the database
        public async Task DeleteTokensAsync(int userId)
        {
            var userTokens = await _context.UserTokens.Where(t => t.UserId == userId).ToListAsync();
            _context.UserTokens.RemoveRange(userTokens);
            await _context.SaveChangesAsync();
        }
    }
}
