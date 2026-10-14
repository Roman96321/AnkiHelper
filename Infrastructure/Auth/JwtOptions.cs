using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Auth
{
    internal class JwtOptions
    {
        public int ExpiresInMinutes { get; set; }
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
    }
}
