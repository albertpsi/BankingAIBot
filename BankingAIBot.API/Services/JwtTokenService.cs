using BankingAIBot.API.Models;
using BankingAIBot.API.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BankingAIBot.API.Services;

public record AuthUserDto(int UserId, string Name, string Email);
public record AuthPayload(string Token, DateTime Expiration, AuthUserDto User);

public interface IJwtTokenService
{
    AuthPayload CreateAuthPayload(User user);
}

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public AuthPayload CreateAuthPayload(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_options.ExpirationMinutes),
            signingCredentials: creds
        );

        return new AuthPayload(
            new JwtSecurityTokenHandler().WriteToken(token),
            token.ValidTo,
            new AuthUserDto(user.UserId, user.Name, user.Email));
    }
}
