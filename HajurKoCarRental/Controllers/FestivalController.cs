using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HajurKoCarRental.Data;
using HajurKoCarRental.Models;
using HajurKoCarRental.Services;

namespace HajurKoCarRental.Controllers
{
    [Authorize(Roles = "Admin")]
    public class FestivalController : Controller
    {
        private readonly FestivalService _festivalService;
        
        public FestivalController(FestivalService festivalService)
        {
            _festivalService = festivalService;
        }
        
        // GET: Festival/Index
        public async Task<IActionResult> Index()
        {
            var festivals = await _festivalService.GetAllFestivalsAsync();
            return View(festivals);
        }
        
        // GET: Festival/Create
        public IActionResult Create()
        {
            return View();
        }
        
        // POST: Festival/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Festival festival)
        {
            if (ModelState.IsValid)
            {
                await _festivalService.CreateFestivalAsync(festival);
                TempData["Success"] = "Festival created successfully!";
                return RedirectToAction(nameof(Index));
            }
            
            return View(festival);
        }
        
        // GET: Festival/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var festival = await _festivalService.GetFestivalByIdAsync(id);
            if (festival == null)
            {
                return NotFound();
            }
            
            return View(festival);
        }
        
        // POST: Festival/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Festival festival)
        {
            if (id != festival.Id)
            {
                return NotFound();
            }
            
            if (ModelState.IsValid)
            {
                await _festivalService.UpdateFestivalAsync(festival);
                TempData["Success"] = "Festival updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            
            return View(festival);
        }
        
        // POST: Festival/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _festivalService.DeleteFestivalAsync(id);
            if (result)
            {
                TempData["Success"] = "Festival deleted successfully!";
            }
            else
            {
                TempData["Error"] = "Festival not found or could not be deleted.";
            }
            
            return RedirectToAction(nameof(Index));
        }
    }
}
