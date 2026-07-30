using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HajurKoCarRental.Services;

namespace HajurKoCarRental.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AnalyticsController : Controller
    {
        private readonly AnalyticsService _analyticsService;

        public AnalyticsController(AnalyticsService analyticsService)
        {
            _analyticsService = analyticsService;
        }

        public async Task<IActionResult> Index()
        {
            var data = await _analyticsService.GetDashboardDataAsync();
            return View(data);
        }
    }
}
