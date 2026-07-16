using EventManager.Domain.Entities;
using EventManager.Infrastructure.Repositories;
using EventManagerAPI.IntegrationTests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EventManagerAPI.IntegrationTests;

[Collection("DatabaseTestCollection")]
public class UserRepositoryTests : IntegrationTestBase
{
	public UserRepositoryTests(PostgreSqlFixture fixture) : base(fixture) { }

	[Fact]
	public async Task Add_ShouldPersistUserToDatabase()
	{
		// Arrange
		var login = "testuser_" + Guid.NewGuid();
		var user = new User(login, "hash123", Role.User);

		// Act
		await UserRepository.AddAsync(user, CancellationToken.None);
		await UserRepository.SaveChangesAsync(CancellationToken.None);

		// Assert
		var savedUser = await UserRepository.GetByLoginAsync(login, CancellationToken.None);
		Assert.NotNull(savedUser);
		Assert.Equal(user.Id, savedUser.Id);
		Assert.Equal(Role.User, savedUser.Role);
	}

	[Fact]
	public async Task Add_WithDuplicateLogin_ShouldThrowDbUpdateException()
	{
		// Arrange
		var login = "duplicate_" + Guid.NewGuid();
		var user1 = new User(login, "hash1", Role.User);
		var user2 = new User(login, "hash2", Role.Admin);

		await UserRepository.AddAsync(user1, CancellationToken.None);
		await UserRepository.SaveChangesAsync(CancellationToken.None);

		// Act
		// Пытаемся добавить второго пользователя с тем же логином
		await UserRepository.AddAsync(user2, CancellationToken.None);

		// Assert
		// Ожидаем исключение от PostgreSQL (нарушение уникального индекса)
		await Assert.ThrowsAsync<DbUpdateException>(() => UserRepository.SaveChangesAsync(CancellationToken.None));
	}

	[Fact]
	public async Task GetByLogin_WhenUserExists_ShouldReturnUser()
	{
		// Arrange
		var login = "findme_" + Guid.NewGuid();
		var user = new User(login, "hash123", Role.Admin);

		await UserRepository.AddAsync(user, CancellationToken.None);
		await UserRepository.SaveChangesAsync(CancellationToken.None);

		// Act
		var foundUser = await UserRepository.GetByLoginAsync(login, CancellationToken.None);

		// Assert
		Assert.NotNull(foundUser);
		Assert.Equal(user.Id, foundUser.Id);
		Assert.Equal(Role.Admin, foundUser.Role);
	}

	[Fact]
	public async Task GetByLogin_WhenUserDoesNotExist_ShouldReturnNull()
	{
		// Arrange
		var nonExistentLogin = "ghost_" + Guid.NewGuid();

		// Act
		var foundUser = await UserRepository.GetByLoginAsync(nonExistentLogin, CancellationToken.None);

		// Assert
		Assert.Null(foundUser);
	}
}