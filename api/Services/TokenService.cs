using System.IdentityModel.Tokens.Jwt;
using System.Text;
using api.Models;
using Microsoft.IdentityModel.Tokens;

namespace api.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _config;
        private readonly SymmetricSecurityKey _key;

        public TokenService(IConfiguration config)
        {
            _config = config;
            var cfg = Environment.GetEnvironmentVariable("JWT_SIGNING_KEY") ?? throw new InvalidOperationException("Signing key must be configured.");
            _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(cfg));
        }

        public string CreateToken(Player user)
        {
            if (user == null || string.IsNullOrEmpty(user.UserName))
                return string.Empty;

            var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256Signature);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Expires = DateTime.Now.AddHours(1),
                SigningCredentials = creds,
                Issuer = _config["JWT:Issuer"],
                Audience = _config["JWT:Audience"]
            };
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}