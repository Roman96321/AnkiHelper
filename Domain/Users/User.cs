using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Users
{
    public class User
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public List<RefreshToken> RefreshTokens { get; set; } = [];
    }
}
