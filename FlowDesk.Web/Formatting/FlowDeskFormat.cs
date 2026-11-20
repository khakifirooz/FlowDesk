using System.Globalization;
using FlowDesk.Application.Common;

namespace FlowDesk.Web.Formatting;

public static class FlowDeskFormat
{
    public static string Number(int number) => PersianDigits(number.ToString(CultureInfo.InvariantCulture));

    public static string DecimalNumber(double number) =>
        PersianDigits(number.ToString("0.##", CultureInfo.InvariantCulture).Replace('.', '٫'));

    public static string IsoDate(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public static string PersianDate(DateOnly date)
    {
        var calendar = new PersianCalendar();
        var value = date.ToDateTime(TimeOnly.MinValue);
        if (value < calendar.MinSupportedDateTime)
            return PersianDigits(IsoDate(date)) + " میلادی";

        return PersianDigits(FormattableString.Invariant(
            $"{calendar.GetYear(value):0000}/{calendar.GetMonth(value):00}/{calendar.GetDayOfMonth(value):00}"));
    }

    public static string TehranDateTime(DateTimeOffset utc)
    {
        var time = FlowDeskCalendar.ToLocal(utc);
        return PersianDate(DateOnly.FromDateTime(time.DateTime))
            + "، " + PersianDigits(time.ToString("HH:mm", CultureInfo.InvariantCulture));
    }

    private static string PersianDigits(string text) =>
        string.Concat(text.Select(c => c is >= '0' and <= '9' ? (char)('۰' + (c - '0')) : c));
}
