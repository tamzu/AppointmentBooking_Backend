using AppointmentBooking.Core.Interfaces.Notifications;

namespace AppointmentBooking.API.BackgroundServices
{
    public class NotificationBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<NotificationBackgroundService> _logger;

        private static readonly TimeSpan PollingInterval =
            TimeSpan.FromSeconds(30);

        public NotificationBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<NotificationBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "Notification background service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();

                    var processor = scope.ServiceProvider
                        .GetRequiredService<INotificationProcessingService>();

                    var processedCount =
                        await processor.ProcessPendingNotificationsAsync(
                            stoppingToken);

                    if (processedCount >= 20)
                    {
                        // There may be more notifications waiting.
                        continue;
                    }
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "An error occurred while processing notification batches.");
                }

                try
                {
                    await Task.Delay(
                        PollingInterval,
                        stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }

            _logger.LogInformation(
                "Notification background service stopped.");
        }
    }
}
