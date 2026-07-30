using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HajurKoCarRental.Data;
using HajurKoCarRental.Models;

namespace HajurKoCarRental.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AuditLogsController : Controller
    {
        private readonly HajurKoCarRentalContext _context;

        public AuditLogsController(HajurKoCarRentalContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var logs = await _context.AuditLogs!
                .Include(l => l.User)
                .OrderByDescending(l => l.Timestamp)
                .ToListAsync();
            return View(logs);
        }
    }
}
