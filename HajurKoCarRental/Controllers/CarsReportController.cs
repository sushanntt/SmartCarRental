using HajurKoCarRental.Areas.Identity.Data;
using HajurKoCarRental.Data;
using HajurKoCarRental.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HajurKoCarRental.Services;
using static HajurKoCarRental.Controllers.HomeController;

namespace HajurKoCarRental.Controllers
{
    // CarsReport class to view reports on cars such as available, unavailable cars
    public class CarsReport : Controller
    {

        private readonly HajurKoCarRentalContext _context;
        private readonly UserManager<HajurKoCarRentalUser> _userManager;
        private readonly SignInManager<HajurKoCarRentalUser> _signInManager;
        private readonly NotificationBadgeService _notificationBadgeService;

        public CarsReport(
            HajurKoCarRentalContext context,
            UserManager<HajurKoCarRentalUser> userManager,
            SignInManager<HajurKoCarRentalUser> signInManager,
            NotificationBadgeService notificationBadgeService)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _notificationBadgeService = notificationBadgeService;
        }
        
        // Redirects user to the index view page of the Cars Report
        public async Task<IActionResult> Index()
        {
            var hajurKoCarRentalContext = _context.Car;
            return View(await hajurKoCarRentalContext!.ToListAsync());
        }

        // Filters the cars according to the search query
        public async Task<IActionResult> FilterData(string searchText)
        {
            var filteredData = new List<Car>();
            if (searchText == "Available")
            {
                
                filteredData = await _context.Car!.Where(c => c.IsCarBooked == false || c.IsCarBooked == null).ToListAsync();
                return View("Index", filteredData);
            }
            else if (searchText == "Unavailable")
            {
                filteredData = await _context.Car!.Where(c => c.IsCarBooked == true).ToListAsync();
                return View("Index", filteredData);
            }
            else if (searchText == "Frequently")
            {
                filteredData = await _context.Car!.ToListAsync();
                var toReturn = new List<Car>();
                foreach (var car in filteredData)
                {
                    if (car.BookedCount >= 3)
                    {
                        toReturn.Add(car);
                    }
                }
                return View("Index", toReturn);
            }
            else if (searchText == "Never")
            {
                filteredData = await _context.Car!.ToListAsync();
                var toReturn = new List<Car>();
                foreach (var car in filteredData)
                {
                    if (car.BookedCount == null || car.BookedCount <= 0)
                    {
                        toReturn.Add(car);
                    }
                }
                return View("Index", toReturn);
            }
            else
            {
                filteredData = await _context.Car!.ToListAsync();
                return View(filteredData);
            }

            return RedirectToAction("Index", "Home");
        }

        // Frequent customer = completed rental days sum >= 15 in the last 3 months
        public async Task<IActionResult> FrequentCustomer()
        {
            await _notificationBadgeService.MarkFrequentCustomersSeenAsync();
            var threeMonthsAgo = DateTime.Now.AddMonths(-3);
            var allUsers = await _context.Users.ToListAsync();
            var result = new List<HajurKoCarRentalUser>();
            foreach (var user in allUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (!roles.Contains("Customer")) continue;
                var totalDays = await _context.RentalHistory!
                    .Where(r => r.UserId == user.Id && r.Status && r.RentedDate >= threeMonthsAgo)
                    .SumAsync(r => r.Duration ?? 0);
                if (totalDays >= 15)
                {
                    user.RegularCustomer = true;
                    result.Add(user);
                }
            }
            return View(result);
        }

        // Inactive customer = no completed rental in the last 1 month
        public async Task<IActionResult> InactiveCustomers()
        {
            var oneMonthAgo = DateTime.Now.AddMonths(-1);
            var allUsers = await _context.Users.ToListAsync();
            var result = new List<HajurKoCarRentalUser>();
            foreach (var user in allUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (!roles.Contains("Customer")) continue;
                var recentCount = await _context.RentalHistory!
                    .CountAsync(r => r.UserId == user.Id && r.Status && r.RentedDate >= oneMonthAgo);
                if (recentCount == 0)
                {
                    result.Add(user);
                }
            }
            return View(result);
        }
    }
}