using EventManager.Application.DTOs.Auth;
using EventManager.Application.Interfaces;
using EventManager.Domain.Entities;
using EventManager.Domain.Exceptions;

namespace EventManager.Application.Services;

/// <summary>
/// Реализация use case сервиса пользователей.
/// Содержит бизнес-логику регистрации и входа в систему.
/// Зависит от Domain (сущности, исключения) и интерфейсов портов (IUserRepository, IPasswordHasher, ITokenService).
/// </summary>
public class UserService : IUserService
{
	private readonly IUserRepository _userRepository;
	private readonly IPasswordHasher _passwordHasher;
	private readonly ITokenService _tokenService;

	public UserService(
		IUserRepository userRepository,
		IPasswordHasher passwordHasher,
		ITokenService tokenService)
	{
		_userRepository = userRepository;
		_passwordHasher = passwordHasher;
		_tokenService = tokenService;
	}

	public async Task RegisterAsync(RegisterUserDto dto, CancellationToken ct)
	{
		var existingUser = await _userRepository.GetByLoginAsync(dto.Login, ct);
		if (existingUser != null)
			throw new DomainValidationException("Пользователь с таким логином уже существует.");

		var hash = _passwordHasher.Hash(dto.Password);
		var user = new User(dto.Login, hash, dto.Role);

		await _userRepository.AddAsync(user, ct);
		await _userRepository.SaveChangesAsync(ct);
	}

	public async Task<TokenResponseDto> LoginAsync(LoginUserDto dto, CancellationToken ct)
	{
		var user = await _userRepository.GetByLoginAsync(dto.Login, ct);

		// Защита от перебора (одно сообщение об ошибке и для неверного логина, и для неверного пароля)
		if (user == null || !_passwordHasher.Verify(dto.Password, user.PasswordHash))
			throw new NotFoundException("Неверные учетные данные.");

		var token = _tokenService.GenerateToken(user);
		return new TokenResponseDto(token);
	}
}