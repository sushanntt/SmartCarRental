using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HajurKoCarRental.Data;
using HajurKoCarRental.Models;
namespace HajurKoCarRental.Controllers
{
    // CarsController class to manage cars on the application
    public class CarsController : Controller
    {
        private readonly HajurKoCarRentalContext _context;
        private readonly IWebHostEnvironment _webHost;

        public CarsController(HajurKoCarRentalContext context, IWebHostEnvironment webHost)
        {
            _context = context;
            _webHost = webHost;
        }

        // GET: Cars
        public async Task<IActionResult> Index()
        {
            var hajurKoCarRentalContext = _context.Car!
                .Include(c => c.CarBrand)
                .Include(c => c.CarBranches)
                    .ThenInclude(cb => cb.Branch);
            return View(await hajurKoCarRentalContext.ToListAsync());
        }

        // GET: Cars/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null || _context.Car == null)
            {
                return NotFound();
            }

            var car = await _context.Car
                .Include(c => c.CarBrand)
                .Include(c => c.CarBranches)
                    .ThenInclude(cb => cb.Branch)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (car == null)
            {
                return NotFound();
            }

            return View(car);
        }

        // GET: Cars/Create
        public IActionResult Create()
        {
            ViewBag.Brands = _context.CarBrand!.Select(b => new SelectListItem{Value = b.Id.ToString(), Text = b.BrandName}).ToList();
            ViewBag.Branches = _context.Branches!.ToList();
            return View();
        }

        // Creates new cars
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Car car, string[] branchIds, int[] branchCounts)
        {
            string uniqueFileName = GetProfilePhotoFileName(car);
            car.PhotoUrl = uniqueFileName;
            car.Id = Guid.NewGuid();

            int totalCount = 0;
            if (branchIds != null)
            {
                for (int i = 0; i < branchIds.Length; i++)
                {
                    if (Guid.TryParse(branchIds[i], out var branchId))
                    {
                        var count = (branchCounts != null && i < branchCounts.Length) ? branchCounts[i] : 1;
                        var carBranchId = Guid.NewGuid();
                        var carBranch = new CarBranch
                        {
                            Id = carBranchId,
                            CarId = car.Id,
                            BranchId = branchId,
                            AvailableCount = count
                        };
                        car.CarBranches.Add(carBranch);
                        totalCount += count;
                    }
                }
            }
            car.AvailableCarCount = totalCount > 0 ? totalCount : 1;

            _context.Add(car);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Car Added Successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Cars/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null || _context.Car == null)
            {
                return NotFound();
            }

            var car = await _context.Car
                .Include(c => c.CarBranches)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (car == null)
            {
                return NotFound();
            }
            ViewBag.Brands = _context.CarBrand!.Select(b => new SelectListItem { Value = b.Id.ToString(), Text = b.BrandName }).ToList();
            ViewBag.Branches = _context.Branches!.ToList();
            return View(car);
        }

        // POST: Cars/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Car car, string[] branchIds, int[] branchCounts)
        {
            if (car.Photo != null)
            {
                string uniqueFileName = GetProfilePhotoFileName(car);
                car.PhotoUrl = uniqueFileName;
            }
            else
            {
                var existingCar = _context.Car.AsNoTracking().FirstOrDefault(c => c.Id == car.Id);
                car.PhotoUrl = existingCar?.PhotoUrl;
            }

            ViewData["BrandId"] = new SelectList(_context.CarBrand, "Id", "Id", car.BrandId);

            // Remove existing CarBranch entries
            var existingBranches = _context.CarBranches!.Where(cb => cb.CarId == car.Id).ToList();
            _context.CarBranches!.RemoveRange(existingBranches);

            // Add new branch assignments
            int totalCount = 0;
            if (branchIds != null)
            {
                for (int i = 0; i < branchIds.Length; i++)
                {
                    if (Guid.TryParse(branchIds[i], out var branchId))
                    {
                        var count = (branchCounts != null && i < branchCounts.Length) ? branchCounts[i] : 1;
                        var carBranchId = Guid.NewGuid();
                        _context.CarBranches!.Add(new CarBranch
                        {
                            Id = carBranchId,
                            CarId = car.Id,
                            BranchId = branchId,
                            AvailableCount = count
                        });
                        totalCount += count;
                    }
                }
            }
            car.AvailableCarCount = totalCount > 0 ? totalCount : 1;

            _context.Attach(car);
            _context.Entry(car).State = EntityState.Modified;
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Car Data Edited Successfully";
            return RedirectToAction(nameof(Index));
        }

        // GET: Cars/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null || _context.Car == null)
            {
                return NotFound();
            }

            var car = await _context.Car
                .Include(c => c.CarBrand)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (car == null)
            {
                return NotFound();
            }

            return View(car);
        }

        // POST: Cars/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            if (_context.Car == null)
            {
                return Problem("Entity set 'HajurKoCarRentalContext.Car'  is null.");
            }
            var car = await _context.Car.FindAsync(id);
            if (car != null)
            {
                _context.Car.Remove(car);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // Checks if the car exists in the database
        private bool CarExists(Guid id)
        {
          return (_context.Car?.Any(e => e.Id == id)).GetValueOrDefault();
        }

        //Copies the selected photo to the images folder of the wwwroot folder and returns the url to the image
        private string GetProfilePhotoFileName(Car car)
        {
            string uniqueFileName = null;

            if (car.Photo != null)
            {
                string uploadsFolder = Path.Combine(_webHost.WebRootPath, "images");
                uniqueFileName = Guid.NewGuid().ToString() + "_" + car.Photo.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    car.Photo.CopyTo(fileStream);
                }
            }
            return uniqueFileName;
        }

    }
}
