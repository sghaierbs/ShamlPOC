namespace Shaml.Application.Abstractions.Security;

public interface ICurrentUser
{
    string SocialId { get; }

    long ActiveUserRoleId { get; }
}