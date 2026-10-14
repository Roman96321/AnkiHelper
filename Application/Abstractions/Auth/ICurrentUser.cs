namespace Application.Abstractions.Auth;

public interface ICurrentUser
{
    Guid UserId { get; }
}
