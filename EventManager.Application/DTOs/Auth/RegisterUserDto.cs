using EventManager.Domain.Entities;

namespace EventManager.Application.DTOs.Auth;

public record RegisterUserDto(string Login, string Password, Role Role = Role.User);