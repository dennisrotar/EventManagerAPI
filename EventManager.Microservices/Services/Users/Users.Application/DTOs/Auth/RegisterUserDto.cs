using Users.Domain.Entities;

namespace Users.Application.DTOs.Auth;

public record RegisterUserDto(string Login, string Password, Role Role = Role.User);