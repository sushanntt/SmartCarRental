namespace HajurKoCarRental.Services
{
    public static class RentalPricingHelper
    {
        /// <summary>
        /// Returns the cost of the hours portion based on hourly tier pricing.
        /// 1hr=20%, 2hr=25%, 3hr=30%, 4hr=35%, 5hr=40%, 6hr=45%, 7hr=50%, 8+hr=100% of dailyRate.
        /// </summary>
        public static double GetHoursCost(double dailyRate, int hours)
        {
            if (hours <= 0) return 0;
            if (hours == 1) return dailyRate * 0.20;
            if (hours == 2) return dailyRate * 0.25;
            if (hours == 3) return dailyRate * 0.30;
            if (hours == 4) return dailyRate * 0.35;
            if (hours == 5) return dailyRate * 0.40;
            if (hours == 6) return dailyRate * 0.45;
            if (hours == 7) return dailyRate * 0.50;
            return dailyRate; // 8+ hours = full day
        }

        /// <summary>
        /// Validates duration+hours. Returns error message or null if valid.
        /// totalHours must be between 1 and 720 (30 days).
        /// </summary>
        public static string? ValidateDuration(int duration, int hours)
        {
            if (duration < 0 || duration > 30)
                return "Duration must be between 0 and 30 days.";
            if (hours < 0 || hours > 23)
                return "Hours must be between 0 and 23.";
            int totalHours = duration * 24 + hours;
            if (totalHours < 1 || totalHours > 720)
                return "Total rental time must be between 1 hour and 30 days.";
            return null;
        }

        /// <summary>
        /// Computes expected return DateTime from rental start + duration days + hours.
        /// </summary>
        public static DateTime ExpectedReturn(DateTime rentalDate, int duration, int hours)
        {
            return rentalDate.AddDays(duration).AddHours(hours);
        }
    }
}
