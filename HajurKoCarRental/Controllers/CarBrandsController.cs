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
    //CarBrandController class to manage car brands
    public class CarBrandsController : Controller
    {
        private readonly HajurKoCarRentalContext _context;
        private readonly IWebHostEnvironment _webHost;

        public CarBrandsController(HajurKoCarRentalContext context, IWebHostEnvironment webHost)
        {
            _context = context;
            _webHost = webHost;
        }

        // GET: CarBrands
        public async Task<IActionResult> Index()
        {
              return _context.CarBrand != null ? 
                          View(await _context.CarBrand.ToListAsync()) :
                          Problem("Entity set 'HajurKoCarRentalContext.CarBrand'  is null.");
        }

        // GET: CarBrands/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null || _context.CarBrand == null)
            {
                return NotFound();
            }

            var carBrand = await _context.CarBrand
                .FirstOrDefaultAsync(m => m.Id == id);
            if (carBrand == null)
            {
                return NotFound();
            }

            return View(carBrand);
        }

        // GET: CarBrands/Create
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(CarBrand carBrand)
        {
            string uniqueFileName = GetProfilePhotoFileName(carBrand);
            carBrand.PhotoUrl = uniqueFileName;

            _context.Add(carBrand);
            _context.SaveChanges();
            TempData["SuccessMessage"] = "Added Successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: CarBrands/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null || _context.CarBrand == null)
            {
                return NotFound();
            }

            var carBrand = await _context.CarBrand.FindAsync(id);
            if (carBrand == null)
            {
                return NotFound();
            }
            return View(carBrand);
        }

        //Edit function to edit the car brands
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(CarBrand carBrand)
        {
            if (carBrand.Photo != null)
            {
                string uniqueFileName = GetProfilePhotoFileName(carBrand);
                carBrand.PhotoUrl = uniqueFileName;
            }
            _context.Attach(carBrand);
            _context.Entry(carBrand).State = EntityState.Modified;
            _context.SaveChanges();
            TempData["SuccessMessage"] = "Edited Successfully";
            return RedirectToAction(nameof(Index));
        }

        // GET: CarBrands/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null || _context.CarBrand == null)
            {
                return NotFound();
            }

            var carBrand = await _context.CarBrand
                .FirstOrDefaultAsync(m => m.Id == id);
            if (carBrand == null)
            {
                return NotFound();
            }

            return View(carBrand);
        }

        // POST: CarBrands/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            if (_context.CarBrand == null)
            {
                return Problem("Entity set 'HajurKoCarRentalContext.CarBrand'  is null.");
            }
            var carBrand = await _context.CarBrand.FindAsync(id);
            if (carBrand != null)
            {
                _context.CarBrand.Remove(carBrand);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        //Checks if the car brands exists
        private bool CarBrandExists(Guid id)
        {
          return (_context.CarBrand?.Any(e => e.Id == id)).GetValueOrDefault();
        }

        //Copies the selected photo to the images folder of the wwwroot folder and returns the url to the image
        private string GetProfilePhotoFileName(CarBrand carBrand)
        {
            string uniqueFileName = null;

            if (carBrand.Photo != null)
            {
                string uploadsFolder = Path.Combine(_webHost.WebRootPath, "images");
                uniqueFileName = Guid.NewGuid().ToString() + "_" + carBrand.Photo.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    carBrand.Photo.CopyTo(fileStream);
                }
            }
            return uniqueFileName;
        }
    }
}
