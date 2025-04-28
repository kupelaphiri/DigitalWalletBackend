using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using BCrypt.Net;
using DigitalWalletAPI.Models;
using DigitalWalletAPI.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authorization;
using System;
using Azure.Core;

namespace DigitalWalletAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // POST api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegisterDto userDto)
        {
            // Check if the user already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == userDto.Email || u.Username == userDto.Username);

            if (existingUser != null)
                return BadRequest("User already exists.");

            var user = new User
            {
                Username = userDto.Username,
                Email = userDto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(userDto.Password)
            };
            var wallet = new Wallet
            {
                UserId = user.Id,
            };

            _context.Users.Add(user);
            _context.Wallets.Add(wallet);
            await _context.SaveChangesAsync();

            var tokens = GenerateTokens(user);

            var userToken = new UserToken
            {
                UserId = user.Id,
                AccessToken = tokens.accessToken,
                RefreshToken = tokens.refreshToken,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                Revoked = false,
            };

            _context.UserTokens.Add(userToken);
            await _context.SaveChangesAsync();

            Response.Cookies.Append("cfcf", tokens.refreshToken, new CookieOptions
             {
             HttpOnly = true,
             Secure = false, 
             SameSite = SameSiteMode.Lax,
             Expires = DateTime.UtcNow.AddDays(7)
             });

           return Ok(new
           {
            message = "User registered successfully!",
            accessToken = tokens.accessToken
            });

        }

        // POST api/auth/login
    [HttpPost("login")]
       public async Task<IActionResult> Login([FromBody] UserLoginDto loginDto)
    {
    var user = await _context.Users
        .FirstOrDefaultAsync(u => u.Email == loginDto.Email && u.Username == loginDto.Username);

    if (user == null)
        return Unauthorized("Invalid username or password.");

    bool passwordMatch = BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash);
    if (!passwordMatch)
        return Unauthorized("Invalid username or password.");

    var tokens = GenerateTokens(user);

    // Save refresh token in the database
    var userToken = new UserToken
    {
        UserId = user.Id,
        AccessToken = tokens.accessToken,
        RefreshToken = tokens.refreshToken,
        CreatedAt = DateTime.UtcNow,
        ExpiresAt = DateTime.UtcNow.AddDays(7),
        Revoked = false
    };

    _context.UserTokens.Add(userToken);
    await _context.SaveChangesAsync();

    // Set the refresh token as an HttpOnly cookie
    Response.Cookies.Append("cfcf", tokens.refreshToken, new CookieOptions
    {
        HttpOnly = true,
        Secure = false, // true in production
        SameSite = SameSiteMode.Lax, // or Lax depending on frontend-backend config
        Expires = DateTime.UtcNow.AddDays(7)
    });

    // Return the access token only (refresh token handled via cookie)
    return Ok(new
    {
        message = "Login successful!",
        accessToken = tokens.accessToken
    });
    }


    [HttpGet("refresh-token")]
     public async Task<IActionResult> RefreshToken()
    {
    // Get refresh token from the cookie
    if (!Request.Cookies.TryGetValue("cfcf", out var refreshToken))
    {
        return Unauthorized("No refresh token found.");
    }

    // Validate the token
    var userToken = await _context.UserTokens
        .FirstOrDefaultAsync(t => t.RefreshToken == refreshToken && !t.Revoked);

    if (userToken == null || userToken.ExpiresAt < DateTime.UtcNow)
    {
        return Unauthorized("Invalid or expired refresh token.");
    }

    // Find the user
    var user = await _context.Users.FindAsync(userToken.UserId);
    if (user == null)
        return Unauthorized("User not found.");

    // Generate new tokens
    var tokens = GenerateTokens(user);

    // Update refresh token (token rotation)
    userToken.RefreshToken = tokens.refreshToken;
    userToken.AccessToken = tokens.accessToken;
    userToken.ExpiresAt = DateTime.UtcNow.AddDays(7);
    await _context.SaveChangesAsync();

    // Set the new refresh token in HttpOnly cookie
    Response.Cookies.Append("cfcf", tokens.refreshToken, new CookieOptions
    {
        HttpOnly = true,
        Secure = false, // Always use HTTPS in production
        SameSite = SameSiteMode.Lax, // Adjust based on your frontend/backend origin relationship
        Expires = DateTime.UtcNow.AddDays(7)
    });

        return Ok(new
        {
         user = user,
         accessToken = tokens.accessToken
         });
    }


        // POST api/auth/logout
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            // Get the userId from the JWT token
            var userIdString = User.FindFirstValue("sub");
            if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
            {
                return Unauthorized("Invalid user ID.");
            }

            // Remove the refresh token from the database
            var userToken = await _context.UserTokens
                .FirstOrDefaultAsync(t => t.UserId == userId);

            if (userToken != null)
            {
                _context.UserTokens.Remove(userToken);
                await _context.SaveChangesAsync();
            }

              // Clear the refresh token cookie
              Response.Cookies.Delete("cfcf");

            return Ok(new { message = "Logged out successfully." });
        }


        // Generate JWT and Refresh Tokens
        private (string accessToken, string refreshToken) GenerateTokens(User user)
        {
            var jwtSettings = _configuration.GetSection("Jwt");

            // Key for signing access token
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Generate the access token
            var claims = new[]
            {
               new Claim(JwtRegisteredClaimNames.Email, user.Email),
               new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
               new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
               new Claim("sub", user.Id.ToString()) // Ensure 'sub' is added
            };

            var accessToken = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(jwtSettings["ExpiryMinutes"])),
                signingCredentials: credentials
            );

            // Generate refresh token (signed JWT with its own key)
            var refreshTokenKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["RefreshTokenKey"])); // Custom key for refresh token
            var refreshTokenCredentials = new SigningCredentials(refreshTokenKey, SecurityAlgorithms.HmacSha256);

            var refreshTokenClaims = new[]
            {
              new Claim("sub", user.Id.ToString()), // Store the user ID in the refresh token
              new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
              new Claim(JwtRegisteredClaimNames.Email, user.Email),
            };

            var refreshToken = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: refreshTokenClaims,
                expires: DateTime.UtcNow.AddDays(Convert.ToDouble(jwtSettings["RefreshTokenExpiryDays"])), // Expiry for refresh token
                signingCredentials: refreshTokenCredentials
            );

            return (new JwtSecurityTokenHandler().WriteToken(accessToken), new JwtSecurityTokenHandler().WriteToken(refreshToken));
        }

    }
}

// DTO for refresh token
public class RefreshTokenDto
{
    public string RefreshToken { get; set; }
}

// User DTOs
public class UserRegisterDto
{
    public string Username { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
}

public class UserLoginDto
{
    public string Username { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
}
