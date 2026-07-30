using HajurKoCarRental.Areas.Identity.Data;
using HajurKoCarRental.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HajurKoCarRental.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly HajurKoCarRentalContext _context;
        private readonly UserManager<HajurKoCarRentalUser> _userManager;

        public UsersController(HajurKoCarRentalContext context, UserManager<HajurKoCarRentalUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var allUsers = await _context.Users.ToListAsync();
            var customers = new List<HajurKoCarRentalUser>();
            foreach (var user in allUsers)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains("Customer"))
                    customers.Add(user);
            }
            return View(customers);
        }

        public async Task<IActionResult> Details(string id)
        {
            if (id == null) return NotFound();
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains("Customer")) return NotFound();
            return View(user);
        }

    }
}
