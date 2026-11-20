namespace FlowDesk.Application.Common;

public static class FlowDeskCalendar
{
    private static readonly TimeZoneInfo Tehran =
        TimeZoneInfo.TryFindSystemTimeZoneById("Asia/Tehran", out var zone)
            ? zone
            : TimeZoneInfo.FindSystemTimeZoneById("Iran Standard Time");

    public static DateOnly Today(TimeProvider clock) =>
        DateOnly.FromDateTime(ToLocal(clock.GetUtcNow()).DateTime);

    public static DateTimeOffset ToLocal(DateTimeOffset utc) =>
        TimeZoneInfo.ConvertTime(utc, Tehran);
}
