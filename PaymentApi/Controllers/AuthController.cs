using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using PaymentApi.Data;
using PaymentApi.Models;
using PaymentApi.Services;

namespace PaymentApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IPasswordHasher _hasher;
        private readonly IJwtTokenService _jwt;

        public AuthController(AppDbContext db, IPasswordHasher hasher, IJwtTokenService jwt)
        {
            _db = db; _hasher = hasher; _jwt = jwt;
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest req)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == req.Username);
            if (user is null)
                return Unauthorized(new { error = "Invalid username or password" });

            if (user.LockoutUntil is not null && user.LockoutUntil > DateTime.UtcNow)
                return Unauthorized(new { error = "Account temporarily locked. Try later." });

            var ok = _hasher.Verify(req.Password, user.PasswordHash);
            if (!ok)
            {
                user.FailedLoginCount += 1;

                if (user.FailedLoginCount >= 5)
                {
                    user.LockoutUntil = DateTime.UtcNow.AddMinutes(10);
                    user.FailedLoginCount = 0;
                }
                await _db.SaveChangesAsync();
                return Unauthorized(new { error = "Invalid username or password" });
            }

            user.FailedLoginCount = 0;
            user.LockoutUntil = null;
            await _db.SaveChangesAsync();

            var (token, expires, jti) = _jwt.CreateToken(user.Id, user.Username);
            return Ok(new LoginResponse { Token = token, ExpiresAt = expires });
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var jti = User.FindFirst("jti")?.Value;
            var expClaim = User.FindFirst("exp")?.Value;

            if (string.IsNullOrEmpty(jti) || string.IsNullOrEmpty(expClaim))
                return BadRequest(new { error = "No token context to logout" });

            var expUnix = long.Parse(expClaim);
            var expiresAt = DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime;

            if (!await _db.RevokedTokens.AnyAsync(r => r.Jti == jti))
            {
                _db.RevokedTokens.Add(new RevokedToken
                {
                    Jti = jti,
                    ExpiresAt = expiresAt
                });
                await _db.SaveChangesAsync();
            }
            return Ok(new { message = "Logged out" });
        }
    }
}
