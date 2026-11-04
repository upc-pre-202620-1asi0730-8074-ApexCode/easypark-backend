using EasyPark.Platform.Reservations.Application.CommandServices;
using EasyPark.Platform.Reservations.Domain.Model.Commands;

namespace EasyPark.Platform.Reservations.Infrastructure.BackgroundJobs;

/// <summary>
///     Periodically expires CONFIRMED reservations whose time window ended without an entry and frees their
///     spots. Configuration: <c>Reservations:ExpirationWorkerEnabled</c>, <c>Reservations:ExpirationIntervalSeconds</c>
///     and <c>Reservations:ExpirationGraceMinutes</c>.
/// </summary>
public class ReservationExpirationWorker(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<ReservationExpirationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!configuration.GetValue("Reservations:ExpirationWorkerEnabled", true))
        {
            logger.LogInformation("Reservation expiration worker disabled by configuration");
            return;
        }

        var interval = TimeSpan.FromSeconds(Math.Max(15,
            configuration.GetValue("Reservations:ExpirationIntervalSeconds", 60)));
        var graceMinutes = Math.Max(0, configuration.GetValue("Reservations:ExpirationGraceMinutes", 0));

        using var timer = new PeriodicTimer(interval);
        do
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var service = scope.ServiceProvider.GetRequiredService<IReservationCommandService>();
                await service.Handle(new ExpireOverdueReservationsCommand(DateTime.UtcNow, graceMinutes), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                // The database can be temporarily unavailable (for example a free MySQL instance waking up).
                logger.LogWarning(exception, "Reservation expiration run failed; it will be retried");
            }
        } while (await WaitForNextTickAsync(timer, stoppingToken));
    }

    private static async Task<bool> WaitForNextTickAsync(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        try
        {
            return await timer.WaitForNextTickAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
