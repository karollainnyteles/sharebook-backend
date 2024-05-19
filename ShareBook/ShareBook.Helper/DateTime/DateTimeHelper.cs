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
            return GetDateTimeNowSaoPaulo().TimeOfDay;
        }

        // data hora agora.
        public static DateTime GetDateTimeNowSaoPaulo() => TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.FindSystemTimeZoneById(SaoPauloTimezoneId));

        // data-hora de hoje a meia noite.
        public static DateTime GetTodaySaoPaulo()
        {
            return GetDateTimeNowSaoPaulo().Date;
        }

        public static DateTime ConvertDateTimeSaoPaulo(DateTime d) => TimeZoneInfo.ConvertTime(d, TimeZoneInfo.FindSystemTimeZoneById(SaoPauloTimezoneId));
    }
}