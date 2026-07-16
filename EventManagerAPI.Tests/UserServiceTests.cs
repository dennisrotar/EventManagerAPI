using EventManager.Application.DTOs.Auth;
using EventManager.Application.Interfaces;
using EventManager.Application.Services;
using EventManager.Domain.Entities;
using EventManager.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using Moq;
using System.Timers;
using Xunit;

namespace EventManagerAPI.Tests;

public class UserServiceTests
{
	private readonly Mock<IUserRepository> _userRepoMock;
	private readonly Mock<IPasswordHasher> _passwordHasherMock;
	private readonly Mock<ITokenService> _tokenServiceMock;
	private readonly UserService _userService;

	public UserServiceTests()
	{
		_userRepoMock = new Mock<IUserRepository>();
		_passwordHasherMock = new Mock<IPasswordHasher>();
		_tokenServiceMock = new Mock<ITokenService>();

		_userService = new UserService(
			_userRepoMock.Object,
			_passwordHasherMock.Object,
			_tokenServiceMock.Object);
	}

	[Fact]
	public async Task RegisterAsync_NewUser_RegistersSuccessfully()
	{
		// Arrange
		var dto = new RegisterUserDto("newuser", "password123", Role.User);
		_userRepoMock.Setup(r => r.GetByLoginAsync(dto.Login, It.IsAny<CancellationToken>()))
			.ReturnsAsync((User?)null);
		_passwordHasherMock.Setup(p => p.Hash(dto.Password)).Returns("hashed_password");

		// Act
		await _userService.RegisterAsync(dto, CancellationToken.None);

		// Assert
		_userRepoMock.Verify(r => r.AddAsync(It.Is<User>(u => u.Login == "newuser" && u.PasswordHash == "hashed_password"), It.IsAny<CancellationToken>()), Times.Once);
		_userRepoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task RegisterAsync_ExistingLogin_ThrowsDomainValidationException()
	{
		// Arrange
		var dto = new RegisterUserDto("existinguser", "password123", Role.User);
		var existingUser = new User("existinguser", "hash", Role.User);
		_userRepoMock.Setup(r => r.GetByLoginAsync(dto.Login, It.IsAny<CancellationToken>()))
			.ReturnsAsync(existingUser);

		// Act & Assert
		await Assert.ThrowsAsync<DomainValidationException>(() => _userService.RegisterAsync(dto, CancellationToken.None));
	}

	[Fact]
	public async Task LoginAsync_ValidCredentials_ReturnsToken()
	{
		// Arrange
		var dto = new LoginUserDto("validuser", "password123");
		var user = new User("validuser", "hashed_password", Role.User);

		_userRepoMock.Setup(r => r.GetByLoginAsync(dto.Login, It.IsAny<CancellationToken>()))
			.ReturnsAsync(user);
		_passwordHasherMock.Setup(p => p.Verify(dto.Password, user.PasswordHash))
			.Returns(true);
		_tokenServiceMock.Setup(t => t.GenerateToken(user))
			.Returns("jwt_token_string");

		// Act
		var result = await _userService.LoginAsync(dto, CancellationToken.None);

		// Assert
		Assert.Equal("jwt_token_string", result.Token);
	}

	[Fact]
	public async Task LoginAsync_InvalidPassword_ThrowsNotFoundException()
	{
		// Arrange
		var dto = new LoginUserDto("validuser", "wrong_password");
		var user = new User("validuser", "hashed_password", Role.User);

		_userRepoMock.Setup(r => r.GetByLoginAsync(dto.Login, It.IsAny<CancellationToken>()))
			.ReturnsAsync(user);
		_passwordHasherMock.Setup(p => p.Verify(dto.Password, user.PasswordHash))
			.Returns(false); // Пароль не совпал

		// Act & Assert
		// Ревьюер просил защиту от перебора: одна ошибка и для неверного логина, и для неверного пароля
		await Assert.ThrowsAsync<NotFoundException>(() => _userService.LoginAsync(dto, CancellationToken.None));
	}

	[Fact]
	public async Task LoginAsync_NonExistentUser_ThrowsNotFoundException()
	{
		// Arrange
		var dto = new LoginUserDto("ghostuser", "password123");
		_userRepoMock.Setup(r => r.GetByLoginAsync(dto.Login, It.IsAny<CancellationToken>()))
			.ReturnsAsync((User?)null); // Пользователь не найден

		// Act & Assert
		await Assert.ThrowsAsync<NotFoundException>(() => _userService.LoginAsync(dto, CancellationToken.None));
	}
}