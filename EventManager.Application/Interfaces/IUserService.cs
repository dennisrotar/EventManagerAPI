using EventManager.Application.DTOs.Auth;

namespace EventManager.Application.Interfaces;

/// <summary>
/// Порт-интерфейс для сервиса пользователей.
/// Содержит бизнес-логику регистрации и аутентификации.
/// </summary>
public interface IUserService
{
	/// <summary>
	/// Регистрирует нового пользователя с хешированием пароля.
	/// </summary>
	Task RegisterAsync(RegisterUserDto dto, CancellationToken ct);

	/// <summary>
	/// Аутентифицирует пользователя и возвращает JWT-токен.
	/// </summary>
	Task<TokenResponseDto> LoginAsync(LoginUserDto dto, CancellationToken ct);
}