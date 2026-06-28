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
            var jwtKey = config["Jwt:Key"] ?? throw new ArgumentNullException("Jwt:Key is missing from settings");
            _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            _issuer = config["Jwt:Issuer"] ?? throw new ArgumentNullException("Jwt:Issuer is missing from settings");
            _audience = config["Jwt:Audience"] ?? throw new ArgumentNullException("Jwt:Audience is missing from settings");
            _durationInMinutes = config.GetValue<int>("Jwt:DurationInMinutes", 60);
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
    }
}
