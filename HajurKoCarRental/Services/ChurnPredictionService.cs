using HajurKoCarRental.Data;
using HajurKoCarRental.Models;
using Microsoft.EntityFrameworkCore;

namespace HajurKoCarRental.Services
{
    public class CustomerChurnRecord
    {
        public string UserId { get; set; }
        public string CustomerName { get; set; }
        public string Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? ProfilePhotoUrl { get; set; }
        public double RecencyScore { get; set; }
        public double FrequencyScore { get; set; }
        public double MonetaryScore { get; set; }
        public double ChurnRiskScore { get; set; }
        public string RiskLevel { get; set; }
        public int DaysSinceLastRental { get; set; }
        public int RentalCount { get; set; }
        public double TotalSpent { get; set; }
        public bool HasUnpaidDamage { get; set; }
        public bool IsProfileComplete { get; set; }
        public bool IsRegularCustomer { get; set; }
    }

    public class ChurnPredictionService
    {
        private readonly HajurKoCarRentalContext _context;
        private const double RECENCY_WEIGHT = 0.40;
        private const double FREQUENCY_WEIGHT = 0.35;
        private const double MONETARY_WEIGHT = 0.25;
        private const int MAX_DAYS = 365;
        private const int FREQUENCY_THRESHOLD = 10;
        private const double MONETARY_THRESHOLD = 50000;
        private const double HIGH_RISK_THRESHOLD = 0.70;
        private const double MEDIUM_RISK_THRESHOLD = 0.40;

        public ChurnPredictionService(HajurKoCarRentalContext context)
        {
            _context = context;
        }

        public async Task<List<CustomerChurnRecord>> GetChurnPredictionsAsync()
        {
            var customerRoleId = await _context.Roles
                .Where(r => r.Name == "Customer")
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            if (customerRoleId == null) return new List<CustomerChurnRecord>();

            var customerUserIds = await _context.UserRoles
                .Where(ur => ur.RoleId == customerRoleId)
                .Select(ur => ur.UserId)
                .ToListAsync();

            if (customerUserIds.Count == 0) return new List<CustomerChurnRecord>();

            var customers = await _context.Users
                .Where(u => customerUserIds.Contains(u.Id))
                .ToListAsync();

            var now = DateTime.Today;

            var allRents = await _context.Rent!
                .Where(r => r.Verified)
                .Select(r => new { r.UserId, r.RentalDate, r.Amount, r.CarId })
                .ToListAsync();

            var allHistory = await _context.RentalHistory!
                .Where(h => h.Status)
                .Select(h => new { h.UserId, RentedDate = h.RentedDate ?? DateTime.MinValue, CarId = h.carId, h.Amount })
                .ToListAsync();

            var allDamage = await _context.CarDamage!
                .Select(d => new { d.UserId, d.DamagePaid })
                .ToListAsync();

            var oneYearAgo = now.AddYears(-1);

            var results = new List<CustomerChurnRecord>();

            foreach (var customer in customers)
            {
                var userRents = allRents.Where(r => r.UserId == customer.Id).ToList();
                var userHistory = allHistory.Where(h => h.UserId == customer.Id).ToList();

                var allDates = userRents.Select(r => r.RentalDate)
                    .Concat(userHistory.Select(h => h.RentedDate))
                    .ToList();

                var historyAmount = userHistory.Sum(h => h.Amount);
                var rentAmount = userRents.Sum(r => r.Amount);
                var totalSpent = historyAmount > 0 ? historyAmount : rentAmount;

                var recentRents = userRents.Where(r => r.RentalDate >= oneYearAgo).ToList();
                var recentHistory = userHistory.Where(h => h.RentedDate >= oneYearAgo).ToList();
                var rentalCount = recentRents.Count + recentHistory.Count;

                int daysSinceLastRental;
                if (!allDates.Any()) continue;

                var lastDate = allDates.Max();
                daysSinceLastRental = (int)(now - lastDate).TotalDays;
                if (daysSinceLastRental < 0) daysSinceLastRental = 0;

                var userDamage = allDamage.Where(d => d.UserId == customer.Id).ToList();
                var hasUnpaidDamage = userDamage.Any(d => !d.DamagePaid);

                double recencyScore = 1.0 - (Math.Min(daysSinceLastRental, MAX_DAYS) / (double)MAX_DAYS);
                double frequencyScore = Math.Min(rentalCount, FREQUENCY_THRESHOLD) / (double)FREQUENCY_THRESHOLD;
                double monetaryScore = Math.Min(totalSpent, MONETARY_THRESHOLD) / MONETARY_THRESHOLD;

                double churnRisk = 0
                    + RECENCY_WEIGHT * (1.0 - recencyScore)
                    + FREQUENCY_WEIGHT * (1.0 - frequencyScore)
                    + MONETARY_WEIGHT * (1.0 - monetaryScore);

                churnRisk = Math.Round(churnRisk, 4);

                string riskLevel;
                if (churnRisk >= HIGH_RISK_THRESHOLD) riskLevel = "High";
                else if (churnRisk >= MEDIUM_RISK_THRESHOLD) riskLevel = "Medium";
                else riskLevel = "Low";

                results.Add(new CustomerChurnRecord
                {
                    UserId = customer.Id,
                    CustomerName = $"{customer.Firstname ?? ""} {customer.Lastname ?? ""}".Trim(),
                    Email = customer.Email ?? "",
                    PhoneNumber = customer.PhoneNumber,
                    ProfilePhotoUrl = customer.ProfilePhotoUrl,
                    RecencyScore = Math.Round(recencyScore, 4),
                    FrequencyScore = Math.Round(frequencyScore, 4),
                    MonetaryScore = Math.Round(monetaryScore, 4),
                    ChurnRiskScore = churnRisk,
                    RiskLevel = riskLevel,
                    DaysSinceLastRental = daysSinceLastRental,
                    RentalCount = rentalCount,
                    TotalSpent = Math.Round(totalSpent, 2),
                    HasUnpaidDamage = hasUnpaidDamage,
                    IsProfileComplete = customer.IsProfileComplete ?? false,
                    IsRegularCustomer = customer.RegularCustomer ?? false,
                });
            }

            return results.OrderByDescending(r => r.ChurnRiskScore).ToList();
        }
    }
}
