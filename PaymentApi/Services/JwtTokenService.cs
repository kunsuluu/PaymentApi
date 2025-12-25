using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace PaymentApi.Services
{
    public interface IJwtTokenService
    {
        (string token, DateTime expiresAt, string jti) CreateToken(int userId, string username);
    }

    public class JwtTokenService : IJwtTokenService
    {
        private readonly string _issuer;
        private readonly string _audience;
        private readonly SymmetricSecurityKey _key;

        public JwtTokenService(string issuer, string audience, string secret)
        {
            _issuer = issuer;
            _audience = audience;
            _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        }

        public (string token, DateTime expiresAt, string jti) CreateToken(int userId, string username)
        {
            var handler = new JwtSecurityTokenHandler();
            var jti = Guid.NewGuid().ToString("N");
            var expires = DateTime.UtcNow.AddHours(2);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, username),
                new Claim(JwtRegisteredClaimNames.Jti, jti)
            };

            var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: expires,
                signingCredentials: creds);

            return (handler.WriteToken(token), expires, jti);
        }
    }
}
