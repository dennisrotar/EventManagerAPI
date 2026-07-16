using EventManager.Domain.Entities;
namespace EventManager.Application.Interfaces;

public interface ITokenService
{
	string GenerateToken(User user);
}