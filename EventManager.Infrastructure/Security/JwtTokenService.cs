using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EventManager.Application.Interfaces;
using EventManager.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace EventManager.Infrastructure.Security;

public class JwtTokenService : ITokenService
{
	private readonly IConfiguration _configuration;

	public JwtTokenService(IConfiguration configuration)
	{
		_configuration = configuration;
	}

	public string GenerateToken(User user)
	{
		var jwtSettings = _configuration.GetSection("Jwt");
		var key = Encoding.UTF8.GetBytes(jwtSettings["Secret"]!);

		var claims = new List<Claim>
		{
			new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
			new Claim(ClaimTypes.Name, user.Login),
			new Claim(ClaimTypes.Role, user.Role.ToString())
		};

		var token = new JwtSecurityToken(
			issuer: jwtSettings["Issuer"],
			audience: jwtSettings["Audience"],
			claims: claims,
			expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(jwtSettings["ExpiresMinutes"])),
			signingCredentials: new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256)
		);

		return new JwtSecurityTokenHandler().WriteToken(token);
	}
}