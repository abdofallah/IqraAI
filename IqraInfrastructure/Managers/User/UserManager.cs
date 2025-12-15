using IqraCore.Entities.App.Configuration;
using IqraCore.Entities.Helpers;
using IqraCore.Entities.Payment.Transaction;
using IqraCore.Entities.Payment.Transaction.Enums;
using IqraCore.Entities.User;
using IqraCore.Entities.User.Billing;
using IqraCore.Entities.User.Billing.Enums;
using IqraCore.Models.Authentication;
using IqraCore.Models.User.Billing;
using IqraInfrastructure.Helpers.User;
using IqraInfrastructure.Managers.Billing;
using IqraInfrastructure.Managers.Mail;
using IqraInfrastructure.Repositories.App;
using IqraInfrastructure.Repositories.Payment;
using IqraInfrastructure.Repositories.User;
using Konscious.Security.Cryptography;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
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
        private readonly UserApiKeyProcessor _apiKeyProcessor;
        private readonly PlanManager _planManager;
        private readonly PaymentTransactionRepository _paymentTransactionRepository;

        private readonly int _sessionDurationHours = 24;

        public UserManager(
            ILogger<UserManager> logger,
            AppRepository appRepository,
            UserSessionRepository userSessionRepository,
            UserRepository userRepository,
            EmailManager emailManager,
            UserApiKeyProcessor apiKeyProcessor,
            PlanManager planManager,
            PaymentTransactionRepository paymentTransactionRepository
        )
        {
            _logger = logger;

            _appRepository = appRepository;
            _userDatabase = userRepository;
            _userSessionDatabase = userSessionRepository;
            _emailManager = emailManager;
            _apiKeyProcessor = apiKeyProcessor;
            _planManager = planManager;
            _paymentTransactionRepository = paymentTransactionRepository;
        }

        // CURD
        public async Task AddBusinessIdToUser(string userEmail, long businessId, IClientSessionHandle mongoSession)
        {
            var updateDefinition = Builders<UserData>.Update
                .AddToSet(u => u.Businesses, businessId);

            await _userDatabase.UpdateUser(userEmail, updateDefinition, mongoSession);
        }

        public async Task<UserData?> GetFullUserByEmail(string email)
        {
            return await _userDatabase.GetFullUserByEmail(email);
        }

        public async Task<UserData?> GetUserDataForLoginValidation(string email)
        {
            return await _userDatabase.GetUserDataForLoginValidation(email);
        }

        public async Task<UserData?> GetUserDataForResetPasswordValidation(string email)
        {
            return await _userDatabase.GetUserDataForResetPasswordValidation(email);
        }

        public async Task<UserData?> GetUserDataForRequestResetPasswordValiation(string email)
        {
            return await _userDatabase.GetUserDataForRequestResetPasswordValiation(email);
        }

        public async Task<bool> CheckUserExistsByEmail(string email)
        {
            return await _userDatabase.CheckUserExistsByEmail(email);
        }

        public async Task<bool> CheckUserIsAdmin(string email)
        {
            return await _userDatabase.CheckUserIsAdmin(email);
        }

        // Management
        public async Task<FunctionReturnResult<UserData?>> RegisterUser(RegisterModel model)
        {
            var result = new FunctionReturnResult<UserData?>();

            BillingPlanConfig? planConfig = await _appRepository.GetBillingPlanConfig();
            if (planConfig == null || planConfig.NewUserCredit < 0 || string.IsNullOrEmpty(planConfig.NewUserPlanId))
            {
                return result.SetFailureResult(
                    "RegisterUser:PLAN_CONFIG_NOT_FOUND",
                    "Plan congfiguration not found or invalid"
                );
            }

            var planDataResult = await _planManager.GetPlanByIdAsync(planConfig.NewUserPlanId);
            if (!planDataResult.Success)
            {
                return result.SetFailureResult(
                    "RegisterUser:PLAN_NOT_FOUND",
                    "Plan not found"
                );
            }

            UserData newUser = new UserData
            {
                Email = model.Email,
                EmailHash = _apiKeyProcessor.ComputeEmailHash(model.Email),
                FirstName = model.FirstName,
                LastName = model.LastName,
                PasswordSHA = HashPassword(model.Email, model.Password),
                Billing = new UserBillingData()
                {
                    CreditBalance = planConfig.NewUserCredit,
                    Subscription = new UserBillingSubscriptionDetails()
                    {
                        PlanId = planConfig.NewUserPlanId,
                        Status = UserBillingSubscriptionStatusEnum.Active
                    }
                }
            };

            var addResult = await _userDatabase.AddUserAsync(newUser);
            if (!addResult)
            {
                return result.SetFailureResult(
                    "RegisterUser:USER_CREATION_FAILED",
                    "User creation failed in db."
                );
            }

            return result.SetSuccessResult(newUser);
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
            string token = ObjectId.GenerateNewId().ToString();

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
            string token = ObjectId.GenerateNewId().ToString();

            var updateDefinition = Builders<UserData>.Update
                .Set(d => d.VerifyEmailToken, token);

            await _userDatabase.UpdateUser(userEmail, updateDefinition);
            return token;
        }

        public async Task<UserSession?> CreateUserSession(string userEmail)
        {
            string sessionId = ObjectId.GenerateNewId().ToString();
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

        public async Task<FunctionReturnResult> ValidateResetPasswordToken(string userEmail, List<UserResetPassword> userResetPasswordTokens, string token)
        {
            var result = new FunctionReturnResult();

            var foundResetPasswordWithToken = userResetPasswordTokens.FirstOrDefault(d => d.Token == token);
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
                var filter = Builders<UserData>.Filter.Eq(u => u.Email, userEmail) & Builders<UserData>.Filter.ElemMatch(u => u.ResetPasswordTokens, d => d.Token == token);
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

        public bool ValidatePassword(string userEmail, string userPasswordSHA, string password)
        {
            try
            {
                // Decode the stored hash+salt
                var storedBytes = Convert.FromBase64String(userPasswordSHA);

                // Extract salt (first 16 bytes) and hash (remaining bytes)
                var salt = new byte[16];
                var storedHash = new byte[storedBytes.Length - 16];
                Array.Copy(storedBytes, 0, salt, 0, 16);
                Array.Copy(storedBytes, 16, storedHash, 0, storedHash.Length);

                // Hash the provided password with the stored salt
                var computedHash = ComputeArgon2Hash(password, userEmail, salt);

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
            return ObjectId.GenerateNewId().ToString();
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
            string body = verifyEmailTemplate.Body.Replace("{{VERIFY_URL}}", verifyUrl);

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
            string body = resetPasswordEmailTemplate.Body.Replace("{{RESET_URL}}", resetUrl);

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
            string body = welcomeEmailTemplate.Body.Replace("{{USER_NAME}}", $"{firstName} {lastName}");

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

        public async Task UpdateLastLoginAndIncreaseCount(string userEmail, UserLoginEntry userLoginEntry)
        {
            var updateDefiniton = Builders<UserData>.Update
                .Set(u => u.Analytics.LastLogin, DateTime.UtcNow)
                .Inc(u => u.Analytics.LoginCount, 1)
                .Push(u => u.Analytics.LoginHistory, userLoginEntry);

            await _userDatabase.UpdateUser(userEmail, updateDefiniton);
        }

        public async Task VerifyUserEmail(string email)
        {
            var updateDefinition = Builders<UserData>.Update
                .Set(u => u.VerifyEmailToken, null);

            await _userDatabase.UpdateUser(email, updateDefinition);
        }

        public async Task<FunctionReturnResult> RemoveBusinessFromUser(string userEmail, long businessId, IClientSessionHandle mongoSession)
        {
            var result = new FunctionReturnResult();

            var updateDefinition = Builders<UserData>.Update
                .Pull(u => u.Businesses, businessId);

            await _userDatabase.UpdateUser(userEmail, updateDefinition, mongoSession);

            return result.SetSuccessResult();
        }

        public async Task<FunctionReturnResult<UserBillingAutoRefillSettings?>> UpdateAutoRefillSettingsAsync(UserData user, UpdateAutoRefillSettingsRequestModel settings)
        {
            var result = new FunctionReturnResult<UserBillingAutoRefillSettings?>();

            try
            {
                if (settings == null)
                {
                    return result.SetFailureResult(
                        "UpdateAutoRefillSettings:INVALID_REQUEST",
                        "Invalid request body."
                    );
                }

                var newAutoRefillSettings = new UserBillingAutoRefillSettings();
                if (settings.Status == UserBillingAutoRefillStatusEnum.Enabled)
                {
                    var planResult = await _planManager.GetPlanByIdAsync(user.Billing.Subscription.PlanId);
                    if (!planResult.Success || planResult.Data == null)
                    {
                        _logger.LogWarning("Could not find plan {PlanId} for user {Email} while updating auto-refill.", user.Billing.Subscription.PlanId, user.Email);
                        return result.SetFailureResult(
                            "UpdateAutoRefillSettings:PLAN_NOT_FOUND",
                            "Your current billing plan could not be found."
                        );
                    }
                    var planData = planResult.Data;

                    if (settings.RefillWhenBalanceBelow == null || settings.RefillWhenBalanceBelow <= 0)
                    {
                        return result.SetFailureResult(
                            "UpdateAutoRefillSettings:INVALID_THRESHOLD",
                            "Threshold amount must be a positive number."
                        );
                    }
                    if (settings.RefillAmount == null || settings.RefillAmount < planData.MinimumTopUpAmount || settings.RefillAmount <= 0)
                    {
                        return result.SetFailureResult(
                            "UpdateAutoRefillSettings:MINIMUM_REFILL_ERROR",
                            $"Refill amount must be at least ${planData.MinimumTopUpAmount}."
                        );
                    }
                    if (string.IsNullOrWhiteSpace(settings.DefaultPaymentMethodId))
                    {
                        return result.SetFailureResult(
                            "UpdateAutoRefillSettings:PAYMENT_METHOD_REQUIRED",
                            "A payment method is required to enable auto-refill."
                        );
                    }

                    var paymentMethodExists = user.PaymentMethods.Any(pm => pm.Id == settings.DefaultPaymentMethodId);
                    if (!paymentMethodExists)
                    {
                        return result.SetFailureResult(
                            "UpdateAutoRefillSettings:PAYMENT_METHOD_NOT_FOUND",
                            "The selected payment method was not found on your account."
                        );
                    }

                    newAutoRefillSettings.Status = UserBillingAutoRefillStatusEnum.Enabled;
                    newAutoRefillSettings.RefillWhenBalanceBelow = settings.RefillWhenBalanceBelow;
                    newAutoRefillSettings.RefillAmount = settings.RefillAmount;
                    newAutoRefillSettings.DefaultPaymentMethodId = settings.DefaultPaymentMethodId;
                }
                else
                {
                    newAutoRefillSettings.Status = UserBillingAutoRefillStatusEnum.Disabled;
                    newAutoRefillSettings.RefillWhenBalanceBelow = null;
                    newAutoRefillSettings.RefillAmount = null;
                    newAutoRefillSettings.DefaultPaymentMethodId = null;
                    newAutoRefillSettings.LastAttemptStatusMessage = null;
                }

                var updateDefinition = Builders<UserData>.Update
                    .Set(u => u.Billing.AutoRefill, newAutoRefillSettings);
                var updateSuccess = await _userDatabase.UpdateUser(user.Email, updateDefinition);
                if (!updateSuccess)
                {
                    _logger.LogError("Failed to update auto-refill settings in database for user {email}", user.Email);
                    return result.SetFailureResult(
                        "UpdateAutoRefillSettings:DB_UPDATE_FAILED",
                        "Could not save settings to the database."
                    );
                }

                return result.SetSuccessResult(newAutoRefillSettings);
            }
            catch (Exception ex) {
                _logger.LogError(ex, "Exception occurred while updating auto-refill settings for user {email}", user.Email);
                return result.SetFailureResult(
                    "UpdateAutoRefillSettings:EXCEPTION",
                    "An unexpected error occurred."
                );
            }
        }

        public async Task<FunctionReturnResult<PaginatedResult<UserBillingHistoryModel>?>> GetBillingHistoryAsync(string userEmail, int limit, string? nextCursor, string? previousCursor)
        {
            var result = new FunctionReturnResult<PaginatedResult<UserBillingHistoryModel>?>();
            var paginatedResult = new PaginatedResult<UserBillingHistoryModel> { PageSize = limit };

            bool fetchNext = string.IsNullOrWhiteSpace(previousCursor);
            string? currentCursor = fetchNext ? nextCursor : previousCursor;
            var decodedCursor = PaginationCursor<PaginationCursorNoFilterHelper>.Decode(currentCursor);

            try
            {
                var (transactions, hasMore) = await _paymentTransactionRepository.GetUserTransactionsPaginatedAsync(userEmail, limit, decodedCursor, fetchNext);
                if (!transactions.Any())
                {
                    return result.SetSuccessResult(new PaginatedResult<UserBillingHistoryModel>());
                }

                paginatedResult.Items = transactions.Select(t => new UserBillingHistoryModel
                {
                    Id = t.Id,
                    CreatedAt = t.CreatedAt,
                    Description = GenerateTransactionDescription(t), // Helper function to generate description
                    USDAmount = t.USDAmount,
                    Status = t.Status,
                    Type = t.Type,
                    FailureReason = t.FailureReason,
                    CardDisplay = t.CardNumber, // Already masked "Visa **** 1234" from payment provider
                    UserPaymentMethodId = t.UserPaymentMethodId,
                    CardHolderName = t.CardHolderName,
                    AddonQuantity = t.FeatureAddonQuantity,
                    AddonUnitPrice = t.FeatureAddonUnitPrice,
                    AddonFeatureKey = t.FeatureAddonKey,
                    AddonId = t.FeatureAddonId
                }).ToList();

                // Cursor logic
                if (fetchNext)
                {
                    paginatedResult.HasNextPage = hasMore;
                    paginatedResult.NextCursor = hasMore ? new PaginationCursor<PaginationCursorNoFilterHelper> { Timestamp = transactions.Last().CreatedAt, Id = transactions.Last().Id }.Encode() : null;
                    paginatedResult.PreviousCursor = decodedCursor != null ? new PaginationCursor<PaginationCursorNoFilterHelper> { Timestamp = transactions.First().CreatedAt, Id = transactions.First().Id }.Encode() : null;
                    paginatedResult.HasPreviousPage = decodedCursor != null;
                }
                else // previous
                {
                    paginatedResult.HasPreviousPage = hasMore;
                    paginatedResult.PreviousCursor = hasMore ? new PaginationCursor<PaginationCursorNoFilterHelper> { Timestamp = transactions.First().CreatedAt, Id = transactions.First().Id }.Encode() : null;
                    paginatedResult.NextCursor = new PaginationCursor<PaginationCursorNoFilterHelper> { Timestamp = transactions.Last().CreatedAt, Id = transactions.Last().Id }.Encode();
                    paginatedResult.HasNextPage = true;
                }

                return result.SetSuccessResult(paginatedResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get billing history for user {Email}", userEmail);
                return result.SetFailureResult("BILLING_HISTORY_FAILED", "An error occurred while fetching billing history.");
            }
        }
        private string GenerateTransactionDescription(PaymentTransaction transaction)
        {
            return transaction.Type switch
            {
                PaymentTransactionTypeEnum.TopUp => "Credit Top-up",
                PaymentTransactionTypeEnum.Subscription => "Monthly Subscription",
                PaymentTransactionTypeEnum.AddCard => "New Card Added (Verification)",
                PaymentTransactionTypeEnum.FeatureAddonPurchase => $"Add-on Purchase",
                PaymentTransactionTypeEnum.FeatureAddonRenewal => $"Add-on Renewal",
                _ => "General Transaction",
            };
        }
    }
}
