using Application.Abstractions.Auth;
using Application.Exceptions;
using Domain.Users;
using System.ComponentModel.DataAnnotations;

namespace Application.UseCases.Auth.Registration
{
    public class UserRegistrationUseCase(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        public async Task Register(RegisterUserRequest request, CancellationToken token)
        {
            var email = NormalizeEmail(request.Email);

            if (!new EmailAddressAttribute().IsValid(email))
                throw new ArgumentException("Email is invalid.");

            if (request.Password.Length < 8)
                throw new ArgumentException("Password must be at least 8 characters long");

            var existingUser = await userRepository.GetUserByEmail(email, token);

            if (existingUser is not null)
                throw new ConflictException("User with this email already exists");

            var hashedPassword = passwordHasher.Generate(request.Password);

            User user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                PasswordHash = hashedPassword,
                CreatedAtUtc = DateTime.UtcNow,
                RefreshTokens = []
            };

            await userRepository.AddUser(user, token);
        }

        private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
    }
}
