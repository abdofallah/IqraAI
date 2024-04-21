using IqraCore.Interfaces.Repositories;
using IqraCore.Models;
using MongoDB.Driver;
using System.Security.Cryptography;
using System.Text;
using IqraCore.Entities.User;
using UserData = IqraCore.Entities.User.User;

namespace IqraInfrastructure.Services.User
{
    public class UserManager
    {
        private readonly IUserSessionRepository _userSessionDatabase;
        private readonly IUserRepository _userDatabase;

        private readonly int _sessionDurationHours = 24;

        public UserManager(IUserSessionRepository userSessionRepository, IUserRepository userRepository)
        {
            _userDatabase = userRepository;
            _userSessionDatabase = userSessionRepository;
        }

        public async Task<UserData?> GetUserByEmail(string email)
        {
            return await _userDatabase.GetUserByEmail(email);
        }

        public async Task<UserData> CreateUser(CreateUserModel model)
        {
            UserData newUser = new UserData
            {
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                PasswordSHA = HashPassword(model.Password),
            };

            await _userDatabase.AddUserAsync(newUser);

            return newUser;
        }

        public async Task<bool> ResetPassword(string userEmail, string newPassword)
        {
            string hashedPassword = HashPassword(newPassword);
            var updateDefinition = Builders<UserData>.Update
                .Set(u => u.PasswordSHA, hashedPassword)
                .Set(u => u.ResetPasswordToken, null)
                .Set(u => u.ResetPasswordExpiry, null);

            return await _userDatabase.UpdateUser(userEmail, updateDefinition);
        }

        public async Task<string> GenerateResetPasswordToken(string userEmail)
        {
            string token = Guid.NewGuid().ToString();
            var updateDefinition = Builders<UserData>.Update
                .Set(u => u.ResetPasswordToken, token)
                .Set(u => u.ResetPasswordExpiry, DateTime.UtcNow.AddHours(1));

            await _userDatabase.UpdateUser(userEmail, updateDefinition);
            return token;
        }

        public async Task<UserSession?> CreateUserSession(string userEmail)
        {
            string sessionId = Guid.NewGuid().ToString();
            string authKey = GenerateAuthKey();

            UserSession userSession = new UserSession
            {
                Id = sessionId,
                Token = authKey
            };

            if (await _userSessionDatabase.CreateSession(userEmail, userSession.Id, userSession.Token, _sessionDurationHours))
            {
                return null;
            }

            return userSession;
        }

        public bool ValidateResetPasswordToken(UserData user, string token)
        {
            return user.ResetPasswordToken == token && user.ResetPasswordExpiry > DateTime.UtcNow;
        }

        public async Task<bool> ValidateSession(string userEmail, string sessionId, string authKey)
        {
            return await _userSessionDatabase.ValidateSession(userEmail, sessionId, authKey);
        }

        public bool ValidatePassword(UserData user, string password)
        {
            string hashedPassword = HashPassword(password);
            return user.PasswordSHA == hashedPassword;
        }

        public string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        public string GenerateAuthKey()
        {
            return Guid.NewGuid().ToString();
        }

        public async Task SendPasswordResetEmail(string userEmail)
        {
            string resetToken = await GenerateResetPasswordToken(userEmail);
            string resetUrl = $"{resetToken}";

            string subject = "Reset Password | ProjectIqra";
            string body = $"<a href='{resetUrl}'>{resetUrl}</a>";

            throw new NotImplementedException();
            //await _emailManager.SendEmailAsync(user.Email, subject, body); todo
        }
    }
}
