namespace Wallhaven.Core;

public enum ScheduleState { NotDue, Changed, Failed }
public sealed record ScheduleOutcome(ScheduleState State, Exception? Error = null);

public sealed class AutoWallpaperSchedule(Func<DateTimeOffset>? clock = null)
{
    private readonly Func<DateTimeOffset> now = clock ?? (() => DateTimeOffset.Now);
    private int running;
    public async Task<ScheduleOutcome> TickAsync(AppSettings settings, Func<CancellationToken, Task> change, CancellationToken ct = default)
    {
        if (!settings.AutoChange || settings.NextChange is not { } due || due > now()) return new(ScheduleState.NotDue);
        if (Interlocked.CompareExchange(ref running, 1, 0) != 0) return new(ScheduleState.NotDue);
        try
        {
            await change(ct);
            settings.NextChange = settings.AutoChange ? now().AddMinutes(Math.Clamp(settings.IntervalMinutes, 1, 1440)) : null;
            return new(ScheduleState.Changed);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            settings.NextChange = settings.AutoChange ? now().AddMinutes(5) : null;
            return new(ScheduleState.Failed, ex);
        }
        finally { Interlocked.Exchange(ref running, 0); }
    }
}
