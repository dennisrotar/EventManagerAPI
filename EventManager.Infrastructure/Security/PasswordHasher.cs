using System.Security.Cryptography;
using System.Text;
using EventManager.Application.Interfaces;

namespace EventManager.Infrastructure.Security;

public class PasswordHasher : IPasswordHasher
{
	public string Hash(string password)
	{
		var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
		return Convert.ToHexString(bytes);
	}

	public bool Verify(string password, string hash)
	{
		var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
		return Convert.ToHexString(bytes) == hash;
	}
}