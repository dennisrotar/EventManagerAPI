using EventManager.Application.DTOs.Auth;
using EventManager.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EventManager.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
	private readonly IUserService _userService;

	public AuthController(IUserService userService)
	{
		_userService = userService;
	}

	[HttpPost("register")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(StatusCodes.Status400BadRequest)]
	public async Task<IActionResult> Register([FromBody] RegisterUserDto dto, CancellationToken ct)
	{
		await _userService.RegisterAsync(dto, ct);
		return NoContent();
	}

	[HttpPost("login")]
	[ProducesResponseType(typeof(TokenResponseDto), StatusCodes.Status200OK)]
	[ProducesResponseType(StatusCodes.Status404NotFound)]
	public async Task<IActionResult> Login([FromBody] LoginUserDto dto, CancellationToken ct)
	{
		var token = await _userService.LoginAsync(dto, ct);
		return Ok(token);
	}
}