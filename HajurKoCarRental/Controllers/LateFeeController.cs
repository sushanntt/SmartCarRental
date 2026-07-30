using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HajurKoCarRental.Data;
using HajurKoCarRental.Models;
using Microsoft.AspNetCore.Identity;
using HajurKoCarRental.Areas.Identity.Data;
using Microsoft.AspNetCore.Authorization;

namespace HajurKoCarRental.Controllers
{
    public class LateFeeController : Controller
    {
        private readonly HajurKoCarRentalContext _context;
        private readonly UserManager<HajurKoCarRentalUser> _userManager;

        public LateFeeController(HajurKoCarRentalContext context, UserManager<HajurKoCarRentalUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Admin: list all late fees
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var lateFees = await _context.LateFees!
                .Include(l => l.User)
                .Include(l => l.Car)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();
            return View(lateFees);
        }

        // Customer: list their late fees
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> MyFees()
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);
            var lateFees = await _context.LateFees!
                .Where(l => l.UserId == user.Id)
                .Include(l => l.Car)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();
            return View(lateFees);
        }

        // Admin: mark as paid
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> MarkPaid(Guid? id)
        {
            if (id == null) return NotFound();
            var lateFee = await _context.LateFees!.FirstOrDefaultAsync(l => l.Id == id);
            if (lateFee == null) return NotFound();

            lateFee.Status = "Paid";
            lateFee.PaidAt = DateTime.Now;
            _context.Update(lateFee);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Late fee marked as paid.";
            return RedirectToAction("Index");
        }
    }
}
