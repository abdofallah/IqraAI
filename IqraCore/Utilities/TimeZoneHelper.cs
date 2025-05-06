using System.Globalization;

namespace IqraCore.Utilities
{
    public static class TimeZoneHelper
    {
        public static TimeSpan? ParseOffsetString(string offsetString)
        {
            if (string.IsNullOrEmpty(offsetString) || offsetString.Length < 6)
                return null;

            char signChar = offsetString[0];
            if (signChar != '+' && signChar != '-')
                return null;

            if (offsetString[3] != ':')
                return null;

            if (int.TryParse(offsetString.Substring(1, 2), NumberStyles.None, CultureInfo.InvariantCulture, out int hours) &&
                int.TryParse(offsetString.Substring(4, 2), NumberStyles.None, CultureInfo.InvariantCulture, out int minutes))
            {
                if (hours >= 0 && hours <= 23 && minutes >= 0 && minutes <= 59)
                {
                    if (signChar == '-')
                    {
                        return new TimeSpan(hours, minutes, 0).Negate();
                    }
                    else
                    {
                        return new TimeSpan(hours, minutes, 0);
                    }
                }
            }

            return null;
        }

        public static bool ValidateOffsetString(string offsetString)
        {
            return ParseOffsetString(offsetString) != null;
        }
    }
}
