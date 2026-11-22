namespace Azma.Application.Abstractions;

/// <summary>Current time in UTC and in the site's local time zone (Iran).</summary>
public interface ISiteClock
{
    DateTime UtcNow { get; }
    DateTime LocalNow { get; }
    DateOnly Today { get; }
    DateTime ToLocal(DateTime utc);
    /// <summary>UTC instant of local midnight at the start of the given local date.</summary>
    DateTime StartOfLocalDayUtc(DateOnly date);
}
