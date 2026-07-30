using HajurKoCarRental.Data;
using HajurKoCarRental.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HajurKoCarRental.Areas.Identity.Data;
using HajurKoCarRental.Services;

namespace HajurKoCarRental.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CustomerDashboardController : Controller
    {
        private readonly HajurKoCarRentalContext _context;
        private readonly UserManager<HajurKoCarRentalUser> _userManager;
        private readonly RecommendationService _recommendationService;
        private readonly FestivalService _festivalService;

        public CustomerDashboardController(
            HajurKoCarRentalContext context,
            UserManager<HajurKoCarRentalUser> userManager,
            RecommendationService recommendationService,
            FestivalService festivalService)
        {
            _context = context;
            _userManager = userManager;
            _recommendationService = recommendationService;
            _festivalService = festivalService;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);
            if (user == null) return Challenge();

            var userId = user.Id;

            var activeRentals = await _context.Rent!
                .Include(r => r.Car)
                .Where(r => r.UserId == userId && r.Verified)
                .OrderByDescending(r => r.RentalDate)
                .ToListAsync();

            var damageClaims = await _context.CarDamage!
                .Include(d => d.Car)
                .Where(d => d.UserId == userId)
                .OrderByDescending(d => d.DamagePaid)
                .ToListAsync();

            var allHistory = await _context.RentalHistory!
                .Where(r => r.UserId == userId)
                .ToListAsync();

            var totalRentals = allHistory.Count(r => r.Status);
            var activeCount = activeRentals.Count;
            var pendingDamages = damageClaims.Count(d => !d.DamagePaid);
            var totalSpent = allHistory.Where(r => r.Status).Sum(r => r.Amount);
            var approvedCount = allHistory.Count(r => r.Status);
            var rejectedCount = allHistory.Count(r => !r.Status);

            var now = DateTime.UtcNow;
            var monthLabels = new List<string>();
            var monthData = new List<int>();
            for (int i = 5; i >= 0; i--)
            {
                var m = now.AddMonths(-i);
                var key = m.ToString("yyyy-MM");
                var label = m.ToString("MMM yyyy");
                monthLabels.Add(label);
                monthData.Add(allHistory.Count(h =>
                    h.Status &&
                    h.RentedDate.HasValue &&
                    h.RentedDate.Value.ToString("yyyy-MM") == key));
            }

            var khaltiCount = allHistory.Count(h => h.PaymentMethod == "Khalti");
            var cashCount = allHistory.Count(h => h.PaymentMethod == "Cash");
            var otherCount = allHistory.Count(h =>
                h.PaymentMethod != "Khalti" && h.PaymentMethod != "Cash" && !string.IsNullOrEmpty(h.PaymentMethod));

            var recommendedCars = await _recommendationService.GetRecommendedCarsAsync(userId, 3);
            var festivalMultiplier = await _festivalService.GetFestivalMultiplierAsync(DateTime.Today);

            ViewBag.TotalRentals = totalRentals;
            ViewBag.ActiveRentals = activeCount;
            ViewBag.PendingDamages = pendingDamages;
            ViewBag.TotalSpent = totalSpent;
            ViewBag.MonthLabels = monthLabels;
            ViewBag.MonthData = monthData;
            ViewBag.ApprovedCount = approvedCount;
            ViewBag.RejectedCount = rejectedCount;
            ViewBag.KhaltiCount = khaltiCount;
            ViewBag.CashCount = cashCount;
            ViewBag.OtherCount = otherCount;
            ViewBag.RecommendedCars = recommendedCars;
            ViewBag.FestivalMultiplier = festivalMultiplier;

            return View(Tuple.Create(activeRentals, damageClaims));
        }
    }
}
