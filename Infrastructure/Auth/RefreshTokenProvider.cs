using Microsoft.AspNetCore.WebUtilities;
using Application.Abstractions.Auth;
using System.Security.Cryptography;
using System.Text;
using System;

namespace Infrastructure.Auth
{
    internal class RefreshTokenProvider : IRefreshTokenProvider
    {
        public string Generate()
        {
            var bytes = RandomNumberGenerator.GetBytes(64);
            return WebEncoders.Base64UrlEncode(bytes);
        }

        public string Hash(string refreshToken)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
            return Convert.ToHexString(bytes);
        }
    }
}
