using Domain.Users;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Abstractions.Auth
{
    public interface IJwtProvider
    {
        string GenerateToken(User user);
    }
}
