using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Abstractions.Auth
{
    public interface IRefreshTokenProvider
    {
        string Generate();
        string Hash(string refreshToken);
    }
}
