using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NovaTech.TerraTech.Platform.CommercialManagement.Application.Services;
using NovaTech.TerraTech.Platform.CommercialManagement.Domain.Model.Commands;
using NovaTech.TerraTech.Platform.CommercialManagement.Domain.Repositories;
using NovaTech.TerraTech.Platform.StockManagement.Domain.Repositories;

namespace NovaTech.TerraTech.Platform.CommercialManagement.Infrastructure.BackgroundJobs;

// US29: evaluates inactivity in the backend, even when no supplier opens Flutter.
// Uses scoped DDD services for each product, and does not bypass ownership checks.
public sealed class ProductInactivityBackgroundService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    IHostEnvironment environment,
    TimeProvider timeProvider,
    ILogger<ProductInactivityBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Integration-test hosts must not create notifications in test databases.
        if (environment.IsEnvironment("Testing") || environment.IsEnvironment("Test"))
        {
            logger.LogInformation("US29 inactivity worker skipped in test environment.");
            return;
        }
        if (!configuration.GetValue("ProductInterest:InactivityWorkerEnabled", true))
        {
            logger.LogInformation("US29 inactivity worker is disabled by configuration.");
            return;
        }

        var initialDelaySeconds = Math.Clamp(
            configuration.GetValue("ProductInterest:InactivityInitialDelaySeconds", 60), 0, 3600);
        var intervalMinutes = Math.Clamp(
            configuration.GetValue("ProductInterest:InactivityIntervalMinutes", 1440), 1, 10080);

        logger.LogInformation(
            "US29 inactivity worker scheduled. InitialDelay={Delay}s, Interval={Interval}m",
            initialDelaySeconds, intervalMinutes);

        try
        {
            // Let the ASP.NET host complete startup / migration initialization first.
            await Task.Delay(TimeSpan.FromSeconds(initialDelaySeconds), timeProvider, stoppingToken);
            while (!stoppingToken.IsCancellationRequested)
            {
                var success = false;
                try
                {
                    success = await EvaluateInactivityAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "US29 inactivity sweep failed; will retry.");
                }

                // Retry transient failures sooner; otherwise check once per day by default.
                var delay = success ? TimeSpan.FromMinutes(intervalMinutes) : TimeSpan.FromMinutes(10);
                await Task.Delay(delay, timeProvider, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal application shutdown.
        }
    }

    private async Task<bool> EvaluateInactivityAsync(CancellationToken cancellationToken)
    {
        List<int> candidateIds;
        await using (var scanScope = scopeFactory.CreateAsyncScope())
        {
            var products = await scanScope.ServiceProvider
                .GetRequiredService<IProductRepository>()
                .ListAsync(cancellationToken);
            var now = timeProvider.GetUtcNow();
            candidateIds = products
                .Where(p => p.ShouldNotifyInactivity(now))
                .Select(p => p.Id)
                .ToList();
        }

        var processed = 0;
        var errors = 0;
        foreach (var productId in candidateIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                // A fresh scope avoids sharing a DbContext after a failed product evaluation.
                await using var scope = scopeFactory.CreateAsyncScope();
                var inventory = await scope.ServiceProvider
                    .GetRequiredService<IInventoryRepository>()
                    .FindByProductIdAsync(productId, cancellationToken);
                if (inventory?.OwnerUserId is not int ownerUserId ||
                    ownerUserId <= 0 || inventory.ProfileId <= 0)
                {
                    logger.LogWarning(
                        "US29 cannot evaluate product {ProductId}: owner/profile not assigned.",
                        productId);
                    continue;
                }

                var result = await scope.ServiceProvider
                    .GetRequiredService<IProductService>()
                    .Handle(new CheckProductInactivityCommand(productId, ownerUserId), cancellationToken);
                if (result.IsSuccess)
                {
                    processed++;
                }
                else
                {
                    errors++;
                    logger.LogWarning("US29 inactivity evaluation failed for product {ProductId}.", productId);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                errors++;
                logger.LogError(ex, "US29 inactivity evaluation error for product {ProductId}.", productId);
            }
        }

        logger.LogInformation(
            "US29 inactivity sweep complete. Candidates={Candidates}, Processed={Processed}, Errors={Errors}",
            candidateIds.Count, processed, errors);
        return errors == 0;
    }
}
