using IqraCore.Entities.User;
using IqraCore.Entities.User.Billing.Enums;
using IqraInfrastructure.Repositories.User;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace IqraInfrastructure.Managers.User
{
    public class UserSubscriptionRenewalService : BackgroundService
    {
        private readonly ILogger<UserSubscriptionRenewalService> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public UserSubscriptionRenewalService(ILogger<UserSubscriptionRenewalService> logger, IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Subscription Renewal Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Subscription Renewal Service is running at: {time}", DateTimeOffset.Now);

                    using (var scope = _serviceScopeFactory.CreateScope())
                    {
                        var userRepository = scope.ServiceProvider.GetRequiredService<UserRepository>();
                        var subscriptionManager = scope.ServiceProvider.GetRequiredService<UserSubscriptionManager>();
                        var now = DateTime.UtcNow;

                        // --- Process Renewals ---
                        _logger.LogInformation("Checking for subscriptions due for renewal.");
                        var renewalFilter = Builders<UserData>.Filter.And(
                            Builders<UserData>.Filter.Eq(u => u.Billing.Subscription.Status, UserBillingSubscriptionStatusEnum.Active),
                            Builders<UserData>.Filter.Lt(u => u.Billing.Subscription.CurrentPeriodEnd, now)
                        );
                        var usersToRenew = await userRepository.GetUsersAsync(renewalFilter); // Assumes repo has this overload

                        foreach (var user in usersToRenew)
                        {
                            try
                            {
                                await subscriptionManager.RenewSubscriptionAsync(user);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error processing renewal for user {UserEmail}", user.Email);
                            }
                        }
                        _logger.LogInformation("Finished processing {Count} renewals.", usersToRenew.Count);

                        // --- Process Cancellations ---
                        _logger.LogInformation("Checking for subscriptions with expired grace periods.");
                        var cancellationFilter = Builders<UserData>.Filter.And(
                            Builders<UserData>.Filter.Eq(u => u.Billing.Subscription.Status, UserBillingSubscriptionStatusEnum.PastDue),
                            Builders<UserData>.Filter.Lt(u => u.Billing.Subscription.GracePeriodExpiresAt, now)
                        );
                        var usersToCancel = await userRepository.GetUsersAsync(cancellationFilter);

                        foreach (var user in usersToCancel)
                        {
                            try
                            {
                                await subscriptionManager.CancelSubscriptionAsync(user, "Payment failed and grace period expired.");
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error processing cancellation for user {UserEmail}", user.Email);
                            }
                        }
                        _logger.LogInformation("Finished processing {Count} cancellations.", usersToCancel.Count);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An unhandled exception occurred in the Subscription Renewal Service.");
                }

                // Wait for the next cycle
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
            _logger.LogInformation("Subscription Renewal Service is stopping.");
        }
    }
}
