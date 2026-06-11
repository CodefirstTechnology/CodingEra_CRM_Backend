using CRM.Services;

namespace CRM.Services
{
    public class LeadSyncAutoSyncHostedService : BackgroundService
    {
        private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(1);
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<LeadSyncAutoSyncHostedService> _logger;

        public LeadSyncAutoSyncHostedService(
            IServiceScopeFactory scopeFactory,
            ILogger<LeadSyncAutoSyncHostedService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunDueSyncsAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Lead sync auto-sync poll failed.");
                }

                try
                {
                    await Task.Delay(PollInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private async Task RunDueSyncsAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var execution = scope.ServiceProvider.GetRequiredService<ILeadSyncExecutionService>();
            var dueIds = await execution.GetDueAutoSyncSourceIdsAsync(cancellationToken);
            foreach (var sourceId in dueIds)
            {
                _logger.LogInformation("Running auto sync for source {SourceId}", sourceId);
                await execution.ExecuteAutoSyncAsync(sourceId, cancellationToken);
            }
        }
    }
}
