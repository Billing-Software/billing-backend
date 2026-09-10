using BillingBackend.Data.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BillingBackend.Services
{
    public class TokenService : ITokenService
    {
        private readonly SymmetricSecurityKey _key;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _durationInMinutes;

        public TokenService(IConfiguration config)
        {
            var jwtKey = config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is missing. Set env var Jwt__Key (min 32 random bytes).");
            var keyBytes = Encoding.UTF8.GetBytes(jwtKey);
            if (keyBytes.Length < 32)
                throw new InvalidOperationException("Jwt:Key must be at least 32 bytes (256 bits) for HS256. Generate a strong random key.");
            _key = new SymmetricSecurityKey(keyBytes);
            _issuer = config["Jwt:Issuer"] ?? throw new ArgumentNullException("Jwt:Issuer is missing from settings");
            _audience = config["Jwt:Audience"] ?? throw new ArgumentNullException("Jwt:Audience is missing from settings");
            _durationInMinutes = config.GetValue<int>("Jwt:DurationInMinutes", 60);
            if (_durationInMinutes <= 0 || _durationInMinutes > 120)
                _durationInMinutes = 60;
        }

        public string CreateToken(User user, int businessId, int? staffId = null)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.NameId, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("businessId", businessId.ToString())
            };

            if (staffId.HasValue)
            {
                claims.Add(new Claim("staffId", staffId.Value.ToString()));
            }

            var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256Signature);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_durationInMinutes),
                SigningCredentials = creds,
                Issuer = _issuer,
                Audience = _audience
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }
}
