namespace Shaml.Application.Identity.RegisterUser;

public sealed record RegisterUserResponse(
    string SocialId,
    string Username,
    string FullName);