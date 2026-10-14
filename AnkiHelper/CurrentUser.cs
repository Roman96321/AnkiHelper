using Application.Abstractions.Auth;
using System.Security.Claims;

namespace AnkiHelper;

internal sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid UserId
    {
        get
        {
            var userId = httpContextAccessor.HttpContext?.User.FindFirstValue("userId");

            if (!Guid.TryParse(userId, out var parsedUserId))
            {
                throw new UnauthorizedAccessException("User id was not found.");
            }

            return parsedUserId;
        }
    }
}
