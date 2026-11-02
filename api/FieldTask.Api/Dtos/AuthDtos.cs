namespace FieldTask.Api.Dtos;

public record LoginDto(string Username, string Password);
public record LoginResultDto(string Token, string Username, string Role);
