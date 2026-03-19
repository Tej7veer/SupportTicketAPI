using Microsoft.AspNetCore.Mvc;
using SupportTicketAPI.Data;
using SupportTicketAPI.Models;
using SupportTicketAPI.Services;
using BCrypt.Net;

namespace SupportTicketAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserRepository _users;
    private readonly JwtService _jwt;

    public AuthController(UserRepository users, JwtService jwt)
    {
        _users = users;
        _jwt = jwt;
    }

    // TEMPORARY — use to generate hashes, remove after
    [HttpGet("hash/{password}")]
    public IActionResult GetHash(string password)
    {
        var hash = BCrypt.Net.BCrypt.HashPassword(password);
        return Ok(new { password, hash });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) ||
            string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(ApiResponse<object>.Fail("Username and password are required."));

        var user = await _users.GetByUsernameAsync(request.Username.Trim());

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(ApiResponse<object>.Fail("Invalid username or password."));

        var token = _jwt.GenerateToken(user);
        var response = new LoginResponse
        {
            Token = token,
            UserId = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            Role = user.Role
        };

        return Ok(ApiResponse<LoginResponse>.Ok(response, "Login successful."));
    }
}
