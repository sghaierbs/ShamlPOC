namespace Shaml.Application.Identity.Login;

public sealed record LoginResponse(
    string AccessToken,
    string TokenType,
    int ExpiresIn,
    long ActiveUserRoleId);