using HajurKoCarRental.Data;
using HajurKoCarRental.Models;
using Microsoft.EntityFrameworkCore;

namespace HajurKoCarRental.Services
{
    public class AnalyticsService
    {
        private readonly HajurKoCarRentalContext _context;
        private readonly ChurnPredictionService _churnService;

        public AnalyticsService(HajurKoCarRentalContext context, ChurnPredictionService churnService)
        {
            _context = context;
            _churnService = churnService;
        }

        public async Task<AnalyticsDashboardData> GetDashboardDataAsync()
        {
            var now = DateTime.Now;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            var startOfYear = new DateTime(now.Year, 1, 1);

            var history = await _context.RentalHistory!.Where(h => h.Status).Include(h => h.Car).ToListAsync();
            var rents = await _context.Rent!.Where(r => r.Verified).Include(r => r.Car).ToListAsync();

            var totalRevenue = history.Sum(h => h.Amount);
            var monthRevenue = history.Where(h => h.RentedDate >= startOfMonth).Sum(h => h.Amount);

            var totalRentals = history.Count;
            var avgPerRental = totalRentals > 0 ? totalRevenue / totalRentals : 0;

            var khaltiRents = history.Where(h => h.PaymentMethod == "Khalti").Sum(h => h.Amount);
            var cashRents = history.Where(h => h.PaymentMethod != "Khalti").Sum(h => h.Amount);

            var cars = await _context.Car!.ToListAsync();
            var totalCars = cars.Count;
            var availableCars = cars.Count(c => c.Available && c.IsCarBooked != true);

            var brands = await _context.CarBrand!.ToListAsync();
            var carsByBrand = brands.Select(b => new
            {
                b.BrandName,
                Count = cars.Count(c => c.BrandId == b.Id)
            }).Where(x => x.Count > 0).ToList();

            var monthlyLabels = new List<string>();
            var monthlyRevenueData = new List<double>();
            var monthlyRentalData = new List<int>();

            for (int i = 1; i <= now.Month; i++)
            {
                var dt = new DateTime(now.Year, i, 1);
                var label = dt.ToString("MMM yyyy");
                monthlyLabels.Add(label);

                var monthStart = dt;
                var monthEnd = monthStart.AddMonths(1);

                var revHistory = history.Where(h => h.RentedDate >= monthStart && h.RentedDate < monthEnd).Sum(h => h.Amount);
                monthlyRevenueData.Add(revHistory);

                var countHistory = history.Count(h => h.RentedDate >= monthStart && h.RentedDate < monthEnd);
                monthlyRentalData.Add(countHistory);
            }

            var activeRentals = await _context.Rent!.CountAsync(r => r.Status == "Approved" || (r.Verified && r.Status == "Pending"));
            var avgDuration = history.Select(h => h.Duration ?? 0).DefaultIfEmpty(0).Average();

            var popular = history
                .Where(h => h.Car != null)
                .GroupBy(h => new { h.carId, ModelName = h.Car.ModelName })
                .Select(g => new { g.Key.ModelName, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToList();

            var customers = await _context.Users
                .Where(u => _context.UserRoles.Any(ur => ur.UserId == u.Id &&
                    _context.Roles.Any(r => r.Id == ur.RoleId && r.Name == "Customer")))
                .ToListAsync();

            var customerIds = customers.Select(c => c.Id).ToHashSet();
            var totalCustomers = customers.Count;

            var allSpenders = history
                .Where(h => customerIds.Contains(h.UserId))
                .GroupBy(h => h.UserId)
                .Select(g => new { UserId = g.Key, Total = g.Sum(x => x.Amount) })
                .OrderByDescending(x => x.Total)
                .ToList();

            var topSpenderNames = allSpenders.Join(customers, ts => ts.UserId, c => c.Id,
                (ts, c) => new { Name = c.Firstname + " " + c.Lastname, ts.Total }).ToList();

            var churnPredictions = await _churnService.GetChurnPredictionsAsync();
            var highRisk = churnPredictions.Count(p => p.RiskLevel == "High");
            var mediumRisk = churnPredictions.Count(p => p.RiskLevel == "Medium");
            var lowRisk = churnPredictions.Count(p => p.RiskLevel == "Low");

            var unpaidDamage = await _context.CarDamage!.Where(d => !d.DamagePaid).ToListAsync();
            var totalDamageCharges = unpaidDamage.Sum(d => d.DamageCharge ?? 0);

            var refundsPaid = await _context.RefundableRent!.Where(r => r.Status == true).SumAsync(r => r.Amount);

            var activeOffers = await _context.Offer!.CountAsync(o => o.Available && o.ValidityPeroid >= now);

            var activeCustomerIds = history.Where(h => h.RentedDate >= startOfMonth).Select(h => h.UserId).Distinct().ToList();
            var activeCustomersThisMonth = activeCustomerIds.Count(customerIds.Contains);
            var regularCustomers = customers.Count(c => c.RegularCustomer == true);
            var customersWithUnpaidDamage = unpaidDamage.Select(d => d.UserId).Distinct().Count();

            return new AnalyticsDashboardData
            {
                TotalRevenue = totalRevenue,
                MonthRevenue = monthRevenue,
                AvgRevenuePerRental = avgPerRental,
                TotalRentals = totalRentals,
                ActiveRentals = activeRentals,
                AvgDuration = avgDuration,
                TotalCars = totalCars,
                AvailableCars = availableCars,
                TotalCustomers = totalCustomers,
                KhaltiRevenue = khaltiRents,
                CashRevenue = cashRents,
                MonthlyLabels = monthlyLabels,
                MonthlyRevenue = monthlyRevenueData,
                MonthlyRentals = monthlyRentalData,
                CarsByBrandLabels = carsByBrand.Select(x => x.BrandName).ToList(),
                CarsByBrandData = carsByBrand.Select(x => x.Count).ToList(),
                PopularCarsLabels = popular.Select(x => x.ModelName).ToList(),
                PopularCarsData = popular.Select(x => x.Count).ToList(),
                TopSpendersLabels = topSpenderNames.Select(x => x.Name).ToList(),
                TopSpendersData = topSpenderNames.Select(x => x.Total).ToList(),
                HighRiskCount = highRisk,
                MediumRiskCount = mediumRisk,
                LowRiskCount = lowRisk,
                TotalDamageCharges = totalDamageCharges,
                UnpaidDamageCharges = unpaidDamage.Count,
                TotalRefundsPaid = refundsPaid,
                ActiveOffersCount = activeOffers,
                MonthRevenueLabel = now.ToString("MMMM yyyy"),
                TotalRevenueLabel = "All Time",
                ActiveCustomersThisMonth = activeCustomersThisMonth,
                RegularCustomers = regularCustomers,
                CustomersWithUnpaidDamage = customersWithUnpaidDamage
            };
        }
    }

    public class AnalyticsDashboardData
    {
        public double TotalRevenue { get; set; }
        public double MonthRevenue { get; set; }
        public double AvgRevenuePerRental { get; set; }
        public int TotalRentals { get; set; }
        public int ActiveRentals { get; set; }
        public double AvgDuration { get; set; }
        public int TotalCars { get; set; }
        public int AvailableCars { get; set; }
        public int TotalCustomers { get; set; }
        public double KhaltiRevenue { get; set; }
        public double CashRevenue { get; set; }
        public string MonthRevenueLabel { get; set; }
        public string TotalRevenueLabel { get; set; }
        public List<string> MonthlyLabels { get; set; }
        public List<double> MonthlyRevenue { get; set; }
        public List<int> MonthlyRentals { get; set; }
        public List<string> CarsByBrandLabels { get; set; }
        public List<int> CarsByBrandData { get; set; }
        public List<string> PopularCarsLabels { get; set; }
        public List<int> PopularCarsData { get; set; }
        public List<string> TopSpendersLabels { get; set; }
        public List<double> TopSpendersData { get; set; }
        public int HighRiskCount { get; set; }
        public int MediumRiskCount { get; set; }
        public int LowRiskCount { get; set; }
        public double TotalDamageCharges { get; set; }
        public int UnpaidDamageCharges { get; set; }
        public double TotalRefundsPaid { get; set; }
        public int ActiveOffersCount { get; set; }
        public int ActiveCustomersThisMonth { get; set; }
        public int RegularCustomers { get; set; }
        public int CustomersWithUnpaidDamage { get; set; }
    }
}
