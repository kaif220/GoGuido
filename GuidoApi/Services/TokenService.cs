using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using GoGuido.Data;
using GoGuido.Models;
using Microsoft.IdentityModel.Tokens;

namespace GoGuido.Services;

public class TokenService(IConfiguration cfg, AppDbContext db)
{
    public async Task<AuthResponse> IssueAsync(User u)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(cfg["Jwt:Key"]!));
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, u.Id.ToString()),
            new Claim(ClaimTypes.Name, u.FullName),
            new Claim(ClaimTypes.Email, u.Email),
            new Claim(ClaimTypes.Role, u.Role)
        };
        var jwt = new JwtSecurityToken(cfg["Jwt:Issuer"], cfg["Jwt:Audience"], claims,
            expires: DateTime.UtcNow.AddMinutes(double.Parse(cfg["Jwt:AccessMinutes"] ?? "60")),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        var rt = new RefreshToken
        {
            UserId = u.Id,
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)),
            ExpiresAt = DateTime.UtcNow.AddDays(double.Parse(cfg["Jwt:RefreshDays"] ?? "14"))
        };
        db.RefreshTokens.Add(rt);
        await db.SaveChangesAsync();
        return new AuthResponse(new JwtSecurityTokenHandler().WriteToken(jwt), rt.Token,
            new UserDto(u.Id, u.FullName, u.Email, u.Role));
    }
}
