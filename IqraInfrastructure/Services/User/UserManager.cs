using MongoDB.Driver;
using System.Security.Cryptography;
using System.Text;
using IqraCore.Entities.User;
using UserData = IqraCore.Entities.User.UserData;
using IqraCore.Models.AppAuthentication;
using IqraCore.Entities.Helpers;
using Serilog;
using IqraInfrastructure.Repositories.User;

namespace IqraInfrastructure.Services.User
{
    public class UserManager
    {
        private readonly UserSessionRepository _userSessionDatabase;
        private readonly UserRepository _userDatabase;

        private readonly int _sessionDurationHours = 24;

        public UserManager(UserSessionRepository userSessionRepository, UserRepository userRepository)
        {
            _userDatabase = userRepository;
            _userSessionDatabase = userSessionRepository;
        }

        public async Task AddBusinessIdToUser(string userEmail, long businessId)
        {
            var updateDefinition = Builders<UserData>.Update
                .AddToSet(u => u.Businesses, businessId);

            await _userDatabase.UpdateUser(userEmail, updateDefinition);
        }

        public async Task<UserData?> GetUserByEmail(string email)
        {
            return await _userDatabase.GetUserByEmail(email);
        }

        public async Task<UserData> RegisterUser(RegisterModel model)
        {
            UserData newUser = new UserData
            {
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                PasswordSHA = HashPassword(model.Password)
            };

            await _userDatabase.AddUserAsync(newUser);

            return newUser;
        }

        public async Task<bool> ResetPassword(string userEmail, string newPassword)
        {
            string hashedPassword = HashPassword(newPassword);
            var updateDefinition = Builders<UserData>.Update
                .Set(u => u.PasswordSHA, hashedPassword);

            return await _userDatabase.UpdateUser(userEmail, updateDefinition);
        }

        public async Task<string> GenerateResetPasswordToken(string userEmail, string? requestedBy)
        {
            string token = Guid.NewGuid().ToString();

            var resetPassword = new UserResetPassword()
            {
                RequestedAt = DateTime.Now,
                Token = token
            };

            if (!string.IsNullOrEmpty(requestedBy)) {
                resetPassword.RequestedBy = requestedBy;
            }

            var updateDefinition = Builders<UserData>.Update
                .AddToSet(d => d.ResetPasswordTokens, resetPassword);

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

            if (!(await _userSessionDatabase.CreateSession(userEmail, userSession.Id, userSession.Token, _sessionDurationHours)))
            {
                return null;
            }

            return userSession;
        }

        public async Task<int> ValidateResetPasswordToken(UserData user, string token)
        {
            UpdateDefinition<UserData>? updateDefinition = null;
            var resultFound = 1; // token not found

            foreach (var userResetPassword in user.ResetPasswordTokens)
            {
                if (userResetPassword.RequestedAt.AddDays(3) < DateTime.UtcNow)
                {
                    updateDefinition = Builders<UserData>.Update
                        .PullFilter(u => u.ResetPasswordTokens, d => (d.Token == userResetPassword.Token && d.RequestedAt == userResetPassword.RequestedAt));
                    await _userDatabase.UpdateUser(user.Email, updateDefinition);

                    continue;
                }

                if (userResetPassword.Token == token)
                {
                    if (userResetPassword.RequestedAt.AddHours(1) < DateTime.UtcNow)
                    {
                        resultFound = 2; // is expired
                    }

                    resultFound = 200; // found and is not expired

                    updateDefinition = Builders<UserData>.Update
                        .PullFilter(u => u.ResetPasswordTokens, d => (d.Token == userResetPassword.Token && d.RequestedAt == userResetPassword.RequestedAt));
                    await _userDatabase.UpdateUser(user.Email, updateDefinition);

                    break;
                }
            }

            return resultFound; 
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

        public async Task SendPasswordResetEmail(string userEmail, string? requestedBy = null)
        {
            string resetToken = await GenerateResetPasswordToken(userEmail, requestedBy);
            string resetUrl = $"{resetToken}";

            string subject = "Reset Password | ProjectIqra";
            string body = $"<a href='{resetUrl}'>{resetUrl}</a>{(string.IsNullOrEmpty(requestedBy) ? "" : $"<p>Requested By: {requestedBy}</p>")}";

            throw new NotImplementedException();
            //await _emailManager.SendEmailAsync(user.Email, subject, body); todo
        }

        public async Task<FunctionReturnResult<List<UserData>?>> GetUsersAsync(int page, int pageSize)
        {
            var result = new FunctionReturnResult<List<UserData>?>();
            result.Data = null;

            var users = await _userDatabase.GetUsersAsync(page, pageSize);
            if (users == null)
            {
                result.Code = "GetUsersAsync:1";
                Log.Logger.Error("[UserManager] Null - Users not found");
            }
            else
            {
                result.Success = true;
                result.Data = users;
            }

            return result;
        }

        public async Task UpdateLastLoginAndIncreaseCount(UserData user)
        {
            var updateDefiniton = Builders<UserData>.Update
                .Set(u => u.Analytics.LastLogin, DateTime.UtcNow)
                .Inc(u => u.Analytics.LoginCount, 1);

            await _userDatabase.UpdateUser(user.Email, updateDefiniton);
        }

        public async Task<bool> addNumberIdToUser(string id, string userEmail)
        {
            var updateDefinition = Builders<UserData>.Update
                .AddToSet(u => u.Numbers, id);

            return await _userDatabase.UpdateUser(userEmail, updateDefinition);
        }
    }
}
