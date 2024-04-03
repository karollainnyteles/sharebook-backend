using System;
using System.Runtime.InteropServices;

namespace ShareBook.Helper
{
    public static class DateTimeHelper
    {
        private static readonly string SaoPauloTimezoneId = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "E. South America Standard Time"
            : "America/Sao_Paulo";

        // hora agora.
        public static TimeSpan GetTimeNowSaoPaulo()
        {
            var now = TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.FindSystemTimeZoneById(SaoPauloTimezoneId));
            var today = new DateTime(now.Year, now.Month, now.Day);
            return now - today;
        }

        // data hora agora.
        public static DateTime GetDateTimeNowSaoPaulo() => TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.FindSystemTimeZoneById(SaoPauloTimezoneId));

        // data-hora de hoje a meia noite.
        public static DateTime GetTodaySaoPaulo()
        {
            var nowSP = GetDateTimeNowSaoPaulo();
            var todaySP = new DateTime(nowSP.Year, nowSP.Month, nowSP.Day, 0, 0, 0);
            return todaySP;
        }

        public static DateTime ConvertDateTimeSaoPaulo(DateTime d) => TimeZoneInfo.ConvertTime(d, TimeZoneInfo.FindSystemTimeZoneById(SaoPauloTimezoneId));
    }
}