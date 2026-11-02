using FieldTask.Api.Data;
using FieldTask.Api.Domain.Entities;
using FieldTask.Api.Dtos;
using FieldTask.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FieldTask.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(FieldTaskDbContext db, IJwtTokenService tokenService) : ControllerBase
{
    private static readonly PasswordHasher<User> Hasher = new();

    [HttpPost("login")]
    public async Task<ActionResult<LoginResultDto>> Login(LoginDto dto, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == dto.Username, ct);
        if (user is null)
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid credentials");

        var result = Hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
        if (result == PasswordVerificationResult.Failed)
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid credentials");

        var token = tokenService.CreateToken(user.Username, user.Role);
        return Ok(new LoginResultDto(token, user.Username, user.Role));
    }
}
