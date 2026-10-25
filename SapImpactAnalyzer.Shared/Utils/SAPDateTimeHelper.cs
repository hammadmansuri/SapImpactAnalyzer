using System.Text.RegularExpressions;

namespace SapImpactAnalyzer.Utils
{
    public static class SAPDateTimeHelper
    {
        public static DateTime ParseSAPDate(string sapDate)
        {
            var match = Regex.Match(sapDate, @"/Date\((\d+)\)/");
            if (match.Success && long.TryParse(match.Groups[1].Value, out long ticks))
            {
                return DateTimeOffset.FromUnixTimeMilliseconds(ticks).DateTime;
            }

            return DateTime.MinValue;
        }

        public static TimeSpan ParseSAPTime(string sapTime)
        {
            return TimeSpan.Parse(sapTime.Replace("PT", "").Replace("H", ":").Replace("M", ":").Replace("S", ":"));
        }
    }
}
