using HajurKoCarRental.Areas.Identity.Data;
using HajurKoCarRental.Data;
using HajurKoCarRental.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using HajurKoCarRental.Services;

namespace HajurKoCarRental.Controllers
{
    // DamageRequest class to manage danage request form
    public class DamageRequest : Controller
    {
        private readonly HajurKoCarRentalContext _context;
        private readonly SignInManager<HajurKoCarRentalUser> _signInManager;
        private readonly UserManager<HajurKoCarRentalUser> _userManager;
        private readonly IWebHostEnvironment _webHost;
        private readonly NotificationBadgeService _notificationBadgeService;

        public DamageRequest(
            HajurKoCarRentalContext context,
            SignInManager<HajurKoCarRentalUser> signInManager,
            UserManager<HajurKoCarRentalUser> userManager,
            IWebHostEnvironment webHost,
            NotificationBadgeService notificationBadgeService)
        {
            _context = context;
            _signInManager = signInManager;
            _userManager = userManager;
            _webHost = webHost;
            _notificationBadgeService = notificationBadgeService;
        }
        
        // Returns all damage requests if the user is admin but returns only request of the user if customer
        public async Task<IActionResult> Index(Guid? carId)
        {
            if (User.IsInRole("Customer"))
            {
                var currentUser = await _userManager.GetUserAsync(HttpContext.User);
                await _notificationBadgeService.MarkCustomerDamageSeenAsync(currentUser!.Id);
                var hajurKoCarRentalContext = _context.CarDamage!.Include(c => c.Car).Include(c => c.User).Where(w => w.UserId == currentUser.Id);
                return View(await hajurKoCarRentalContext!.ToListAsync());
            }

            await _notificationBadgeService.MarkAdminDamageSeenAsync();
            var allDataContext = _context.CarDamage!.Include(c => c.Car).Include(c => c.User);
            return View(await allDataContext!.ToListAsync());
        }

        // Returns to the damage request form view
        public IActionResult Create(Guid carId)
        {
            var car = _context.Car!.FirstOrDefault(c => c.Id == carId);
            ViewData["Car"] = car;
            return View();
        }

        // Adds the request to the database
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CarDamage carDamage, Guid carId)
        {
            var currentUser = await _userManager.GetUserAsync(HttpContext.User);
            string uniqueFileName = GetProfilePhotoFileName(carDamage);
            carDamage.PhotoUrl = uniqueFileName;
            carDamage.UserId = currentUser.Id;
            carDamage.CarId = carId;
            carDamage.AdminSeen = false;
            carDamage.CustomerSeen = true;

            _context.Add(carDamage);
            _context.SaveChanges();

            currentUser.CanBook = false;
            _context.Update(currentUser);
            _context.SaveChanges();

            return RedirectToAction(nameof(Index));
        }

        //Copies the selected photo to the images folder of the wwwroot folder and returns the url to the image
        private string GetProfilePhotoFileName(CarDamage carDamage)
        {
            string uniqueFileName = null;

            if (carDamage.Photo != null)
            {
                string uploadsFolder = Path.Combine(_webHost.WebRootPath, "images");
                uniqueFileName = Guid.NewGuid().ToString() + "_" + carDamage.Photo.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    carDamage.Photo.CopyTo(fileStream);
                }
            }
            return uniqueFileName!;
        }

        // GET: CarDamages/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null || _context.CarDamage == null)
            {
                return NotFound();
            }

            var carDamage = await _context.CarDamage.FindAsync(id);
            if (carDamage == null)
            {
                return NotFound();
            }
            ViewData["CarId"] = new SelectList(_context.Car, "Id", "Id", carDamage.CarId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", carDamage.UserId);
            return View(carDamage);
        }

        // POST: CarDamages/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,DamageDescription,UserId,CarId,PhotoUrl,DamagePaid,DamageCharge")] CarDamage carDamage)
        {
            try
            {
                carDamage.CustomerSeen = false;
                _context.Update(carDamage);
                await _context.SaveChangesAsync();

                var user = _context.Users.FirstOrDefault(u => u.Id == carDamage.UserId);

                user!.CanBook = true;
                _context.Update(user);
                _context.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CarDamageExists(carDamage.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }

        private bool CarDamageExists(Guid id)
        {
            return (_context.CarDamage?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
