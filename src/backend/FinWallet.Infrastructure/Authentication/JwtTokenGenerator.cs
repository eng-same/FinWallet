using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FinWallet.Application.Common.Interfaces;
using FinWallet.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace FinWallet.Infrastructure.Authentication;

public sealed class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly IConfiguration _configuration;

    public JwtTokenGenerator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateToken(ApplicationUser user, IList<string> roles)
    {
        var secretKey = _configuration["Jwt:SecretKey"] ?? "FinWalletSuperSecretJWTKey2026!WithExtraSecurityCharacters";
        var issuer = _configuration["Jwt:Issuer"] ?? "FinWallet.Api";
        var audience = _configuration["Jwt:Audience"] ?? "FinWallet.Frontend";
        var expirationMinutes = double.Parse(_configuration["Jwt:AccessTokenExpirationMinutes"] ?? "120");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("fullName", user.FullName)
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var token = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(expirationMinutes),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var securityToken = tokenHandler.CreateToken(token);

        return tokenHandler.WriteToken(securityToken);
    }
}
