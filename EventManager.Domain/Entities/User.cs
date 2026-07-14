namespace EventManager.Domain.Entities;

public class User
{
	public Guid Id { get; private set; }
	public string Login { get; private set; }
	public string PasswordHash { get; private set; }
	public Role Role { get; private set; }

	private User() { } // Для EF Core

	public User(string login, string passwordHash, Role role)
	{
		Id = Guid.NewGuid();
		Login = login;
		PasswordHash = passwordHash;
		Role = role;
	}
}