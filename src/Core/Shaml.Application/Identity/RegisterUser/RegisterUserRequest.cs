namespace Shaml.Application.Identity.RegisterUser;

public sealed record RegisterUserRequest(
    string SocialId,
    string Username,
    string FullName,
    string Password);