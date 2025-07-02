using Deepgram.Models.Manage.v1;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.User;
using IqraCore.Entities.User.Billing;
using IqraCore.Models.Authentication;
using IqraInfrastructure.Managers.Mail;
using IqraInfrastructure.Repositories.App;
using IqraInfrastructure.Repositories.User;
using Konscious.Security.Cryptography;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System.Security.Cryptography;
using System.Text;
using UserData = IqraCore.Entities.User.UserData;

namespace IqraInfrastructure.Managers.User
{
    public class UserManager
    {
        private readonly ILogger<UserManager> _logger;

        private readonly AppRepository _appRepository;
        private readonly UserSessionRepository _userSessionDatabase;
        private readonly UserRepository _userDatabase;
        private readonly EmailManager _emailManager;

        private readonly int _sessionDurationHours = 24;

        public UserManager(ILogger<UserManager> logger, AppRepository appRepository, UserSessionRepository userSessionRepository, UserRepository userRepository, EmailManager emailManager)
        {
            _logger = logger;

            _appRepository = appRepository;
            _userDatabase = userRepository;
            _userSessionDatabase = userSessionRepository;
            _emailManager = emailManager;
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

        public async Task<UserData> RegisterUser(RegisterModel model, decimal defaultCreditBalance)
        {
            UserData newUser = new UserData
            {
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                PasswordSHA = HashPassword(model.Email, model.Password),
                Billing = new UserBillingData()
                {
                    CreditBalance = defaultCreditBalance,
                    Subscription = null
                }
            };

            await _userDatabase.AddUserAsync(newUser);

            return newUser;
        }

        public async Task<bool> ResetPassword(string userEmail, string newPassword)
        {
            string hashedPassword = HashPassword(userEmail, newPassword);
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
                Token = token,
                RequestedBy = requestedBy,
                IsUsed = false
            };

            var updateDefinition = Builders<UserData>.Update
                .AddToSet(d => d.ResetPasswordTokens, resetPassword);

            await _userDatabase.UpdateUser(userEmail, updateDefinition);
            return token;
        }

        public async Task<string> GenerateUserRegisterVerifyToken(string userEmail)
        {
            string token = Guid.NewGuid().ToString();

            var updateDefinition = Builders<UserData>.Update
                .Set(d => d.VerifyEmailToken, token);

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

        public async Task<FunctionReturnResult> ValidateResetPasswordToken(UserData user, string token)
        {
            var result = new FunctionReturnResult();

            var foundResetPasswordWithToken = user.ResetPasswordTokens.FirstOrDefault(d => d.Token == token);
            if (foundResetPasswordWithToken == null || foundResetPasswordWithToken.IsUsed)
            {
                return result.SetFailureResult(
                    "ValidateResetPasswordToken:1",
                    "Reset password token not found"
                );
            }

            try
            {
                if (foundResetPasswordWithToken.RequestedAt.AddHours(1) < DateTime.UtcNow)
                {
                    return result.SetFailureResult(
                        "ValidateResetPasswordToken:2",
                        "Reset password token is expired"
                    );
                }

                return result.SetSuccessResult();
            }
            finally
            {
                var filter = Builders<UserData>.Filter.Eq(u => u.Email, user.Email) & Builders<UserData>.Filter.ElemMatch(u => u.ResetPasswordTokens, d => d.Token == token);
                var updateDefinition = Builders<UserData>.Update.Set(u => u.ResetPasswordTokens.FirstMatchingElement().IsUsed, true);
                await _userDatabase.UpdateUser(filter, updateDefinition);
            } 
        }

        public async Task<bool> ValidateSession(string userEmail, string sessionId, string authKey)
        {
            return await _userSessionDatabase.ValidateSession(userEmail, sessionId, authKey);
        }

        private byte[] ComputeArgon2Hash(string password, string email, byte[] salt)
        {
            var passwordBytes = Encoding.UTF8.GetBytes(password);
            using (var hasher = new Argon2id(passwordBytes))
            {
                hasher.DegreeOfParallelism = 8;
                hasher.MemorySize = 12000;          // 12 MB (OWASP recommended minimum)
                hasher.Iterations = 10;              // 10 iterations with higher memory
                hasher.Salt = salt;
                hasher.AssociatedData = Encoding.UTF8.GetBytes(email);
                return hasher.GetBytes(32);         // 32 bytes output
            }
        }

        public string HashPassword(string userEmail, string password)
        {
            // Generate unique salt for each password
            var salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            var passwordHash = ComputeArgon2Hash(password, userEmail, salt);

            // Store both salt and hash together
            var result = new byte[salt.Length + passwordHash.Length];
            Array.Copy(salt, 0, result, 0, salt.Length);
            Array.Copy(passwordHash, 0, result, salt.Length, passwordHash.Length);

            return Convert.ToBase64String(result);
        }

        public bool ValidatePassword(UserData user, string password)
        {
            try
            {
                // Decode the stored hash+salt
                var storedBytes = Convert.FromBase64String(user.PasswordSHA);

                // Extract salt (first 16 bytes) and hash (remaining bytes)
                var salt = new byte[16];
                var storedHash = new byte[storedBytes.Length - 16];
                Array.Copy(storedBytes, 0, salt, 0, 16);
                Array.Copy(storedBytes, 16, storedHash, 0, storedHash.Length);

                // Hash the provided password with the stored salt
                var computedHash = ComputeArgon2Hash(password, user.Email, salt);

                // Constant-time comparison to prevent timing attacks
                return CryptographicOperations.FixedTimeEquals(storedHash, computedHash);
            }
            catch
            {
                return false; // Invalid format or other error
            }
        }

        public string GenerateAuthKey()
        {
            return Guid.NewGuid().ToString();
        }

        public async Task<FunctionReturnResult> GenerateAndSendUserRegisterVerifyEmail(string userEmail)
        {
            var result = new FunctionReturnResult();

            string verifyToken = await GenerateUserRegisterVerifyToken(userEmail);
            string verifyUrl = $"https://app.iqra.bot/verify?email={userEmail}&token={verifyToken}";

            var emailTemplates = await _appRepository.GetEmailTemplates();
            if (emailTemplates == null)
            {
                _logger.LogError("Email Templates not found from database while sending user register email, {email}", userEmail);
                return result.SetFailureResult(
                    "SendUserRegisterVerifyEmail:1",
                    "Email Templates not found"
                );
            }

            var verifyEmailTemplate = emailTemplates.VerifyEmailTemplate;
            if (string.IsNullOrEmpty(verifyEmailTemplate.Subject) || string.IsNullOrEmpty(verifyEmailTemplate.Body))
            {
                _logger.LogError("Verify Email Template subject or body null or empty while sending user register email, {email}", userEmail);
                return result.SetFailureResult(
                    "SendUserRegisterVerifyEmail:2",
                    "Verify Email Template subject or body null or empty"
                );
            }

            string subject = verifyEmailTemplate.Subject;
            string body = verifyEmailTemplate.Body.Replace("{{verifyUrl}}", verifyUrl);

            if (!(await _emailManager.SendEmailAsync(userEmail, subject, body)))
            {
                _logger.LogError("Failed to send user register verify email because email manager failed, {email}", userEmail);
                return result.SetFailureResult(
                    "SendUserRegisterVerifyEmail:3",
                    "Failed to send user register verify email"
                );
            }

            return result.SetSuccessResult();
        }

        public async Task<FunctionReturnResult> GenerateAndSendPasswordResetEmail(string userEmail, string? requestedBy = null)
        {
            var result = new FunctionReturnResult();

            string resetToken = await GenerateResetPasswordToken(userEmail, requestedBy);
            string resetUrl = $"https://app.iqra.bot/reset?email={userEmail}&token={resetToken}";

            var emailTemplates = await _appRepository.GetEmailTemplates();
            if (emailTemplates == null)
            {
                _logger.LogError("Email Templates not found from database while sending user register email, {email}", userEmail);
                return result.SetFailureResult(
                    "SendPasswordResetEmail:1",
                    "Email Templates not found"
                );
            }

            var resetPasswordEmailTemplate = emailTemplates.ResetPasswordTemplate;
            if (string.IsNullOrEmpty(resetPasswordEmailTemplate.Subject) || string.IsNullOrEmpty(resetPasswordEmailTemplate.Body))
            {
                _logger.LogError("Reset password Email Template subject or body null or empty while sending user register email, {email}", userEmail);
                return result.SetFailureResult(
                    "SendPasswordResetEmail:2",
                    "Reset password Email Template subject or body null or empty"
                );
            }

            string subject = resetPasswordEmailTemplate.Subject;
            string body = resetPasswordEmailTemplate.Body.Replace("{{resetUrl}}", resetUrl);

            if (!(await _emailManager.SendEmailAsync(userEmail, subject, body)))
            {
                _logger.LogError("Failed to send user register reset password email because email manager failed, {email}", userEmail);
                return result.SetFailureResult(
                    "SendPasswordResetEmail:3",
                    "Failed to send user register reset password email"
                );
            }

            return result.SetSuccessResult();
        }

        public async Task<FunctionReturnResult> SendUserRegisterWelcomeEmail(string userEmail, string firstName, string lastName)
        {
            var result = new FunctionReturnResult();

            var emailTemplates = await _appRepository.GetEmailTemplates();
            if (emailTemplates == null)
            {
                _logger.LogError("Email Templates not found from database while sending user register email, {email}", userEmail);
                return result.SetFailureResult(
                    "SendUserRegisterWelcomeEmail:1",
                    "Email Templates not found"
                );
            }

            var welcomeEmailTemplate = emailTemplates.WelcomeUserTemplate;
            if (string.IsNullOrEmpty(welcomeEmailTemplate.Subject) || string.IsNullOrEmpty(welcomeEmailTemplate.Body))
            {
                _logger.LogError("Welcome Email Template subject or body null or empty while sending user register email, {email}", userEmail);
                return result.SetFailureResult(
                    "SendUserRegisterWelcomeEmail:2",
                    "Welcome Email Template subject or body null or empty"
                );
            }

            string subject = welcomeEmailTemplate.Subject;
            string body = welcomeEmailTemplate.Body.Replace("{{firstName}}", firstName).Replace("{{lastName}}", lastName);

            if (!(await _emailManager.SendEmailAsync(userEmail, subject, body)))
            {
                _logger.LogError("Failed to send user register welcome email because email manager failed, {email}", userEmail);
                return result.SetFailureResult(
                    "SendUserRegisterWelcomeEmail:3",
                    "Failed to send user register welcome email"
                );
            }

            return result.SetSuccessResult();
        }

        public async Task<FunctionReturnResult<List<UserData>?>> GetUsersAsync(int page, int pageSize)
        {
            var result = new FunctionReturnResult<List<UserData>?>();
            result.Data = null;

            var users = await _userDatabase.GetUsersAsync(page, pageSize);
            if (users == null)
            {
                result.Code = "GetUsersAsync:1";
                _logger.LogError("[UserManager] Null - Users not found");
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

        public async Task VerifyUserEmail(string email)
        {
            var updateDefinition = Builders<UserData>.Update
                .Set(u => u.VerifyEmailToken, null);

            await _userDatabase.UpdateUser(email, updateDefinition);
        } 
    }
}
