using SurveilWin.Api.Services;

namespace SurveilWin.Api.BackgroundJobs;

public class ShiftAutoCloseJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<ShiftAutoCloseJob> _logger;

    public ShiftAutoCloseJob(IServiceProvider services, ILogger<ShiftAutoCloseJob> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromHours(1), ct);
            try
            {
                using var scope = _services.CreateScope();
                var shiftService = scope.ServiceProvider.GetRequiredService<IShiftService>();
                await shiftService.AutoCloseStaleShiftsAsync();
                _logger.LogInformation("Shift auto-close job completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in shift auto-close job");
            }
        }
    }
}
