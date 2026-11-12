namespace Shaml.Application.Identity.Login;

public sealed record LoginRequest(
    string Username,
    string Password);