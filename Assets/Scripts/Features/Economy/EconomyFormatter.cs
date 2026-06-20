using System;

namespace TimeAura.Features.Economy
{
    /// <summary>
    /// EconomyFormatter - Static utility for pretty displaying currencies and time balances.
    /// Handles correct Ukrainian grammatical declensions.
    /// </summary>
    public static class EconomyFormatter
    {
        /// <summary>
        /// Formats time in minutes to a readable Ukrainian string.
        /// </summary>
        public static string FormatHoras(long minutes)
        {
            if (minutes < 60)
            {
                return $"{minutes} хв";
            }

            long hours = minutes / 60;
            long remainder = minutes % 60;

            string horaDeclension = GetDeclension(hours, "Хора", "Хори", "Хор");

            if (remainder == 0)
            {
                return $"{hours} {horaDeclension}";
            }

            string minuteDeclension = GetDeclension(remainder, "хвилина", "хвилини", "хвилин");
            return $"{hours} {horaDeclension} {remainder} {minuteDeclension}";
        }

        /// <summary>
        /// Formats Waves (integer Quants * 100) to a string with one decimal place.
        /// </summary>
        public static string FormatQuants(long waves)
        {
            double quants = waves / 100.0;
            string formattedQuants = quants.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
            return $"{formattedQuants} Квантів";
        }

        /// <summary>
        /// Formats cents (USD cents) to a string.
        /// </summary>
        public static string FormatFiat(long cents)
        {
            double dollars = cents / 100.0;
            return $"${dollars:F2}";
        }

        private static string GetDeclension(long value, string form1, string form2, string form5)
        {
            long absVal = Math.Abs(value);
            long lastDigit = absVal % 10;
            long lastTwoDigits = absVal % 100;

            if (lastTwoDigits >= 11 && lastTwoDigits <= 14)
            {
                return form5;
            }
            if (lastDigit == 1)
            {
                return form1;
            }
            if (lastDigit >= 2 && lastDigit <= 4)
            {
                return form2;
            }
            return form5;
        }
    }
}
