using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Users
{
    public class RefreshToken
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public string TokenHash { get; set; } = string.Empty;

        public DateTime ExpiresAtUtc { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? RevokedAtUtc { get; set; }

        public User User { get; set; } = null!;
    }
}
