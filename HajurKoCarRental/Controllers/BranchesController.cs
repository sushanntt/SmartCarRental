using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HajurKoCarRental.Data;
using HajurKoCarRental.Models;
using Microsoft.AspNetCore.Authorization;

namespace HajurKoCarRental.Controllers
{
    [Authorize(Roles = "Admin")]
    public class BranchesController : Controller
    {
        private readonly HajurKoCarRentalContext _context;

        public BranchesController(HajurKoCarRentalContext context)
        {
            _context = context;
        }

        // GET: Branches
        public async Task<IActionResult> Index()
        {
            return View(await _context.Branches!.ToListAsync());
        }

        // GET: Branches/Details/5
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null || _context.Branches == null)
            {
                return NotFound();
            }

            var branch = await _context.Branches
                .FirstOrDefaultAsync(m => m.Id == id);
            if (branch == null)
            {
                return NotFound();
            }

            return View(branch);
        }

        // GET: Branches/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Branches/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Address,Contact,Latitude,Longitude")] Branch branch)
        {
            if (ModelState.IsValid)
            {
                branch.Id = Guid.NewGuid();
                branch.Address ??= "";
                _context.Add(branch);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Branch added successfully.";
                return RedirectToAction(nameof(Index));
            }
            return View(branch);
        }

        // GET: Branches/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null || _context.Branches == null)
            {
                return NotFound();
            }

            var branch = await _context.Branches.FindAsync(id);
            if (branch == null)
            {
                return NotFound();
            }
            return View(branch);
        }

        // POST: Branches/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Name,Address,Contact,Latitude,Longitude")] Branch branch)
        {
            if (id != branch.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    branch.Address ??= "";
                    _context.Update(branch);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Branch updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BranchExists(branch.Id))
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
            return View(branch);
        }

        // GET: Branches/Delete/5
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null || _context.Branches == null)
            {
                return NotFound();
            }

            var branch = await _context.Branches
                .FirstOrDefaultAsync(m => m.Id == id);
            if (branch == null)
            {
                return NotFound();
            }

            return View(branch);
        }

        // POST: Branches/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            if (_context.Branches == null)
            {
                return Problem("Entity set 'HajurKoCarRentalContext.Branches' is null.");
            }
            var branch = await _context.Branches.FindAsync(id);
            if (branch != null)
            {
                _context.Branches.Remove(branch);
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Branch deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        private bool BranchExists(Guid id)
        {
            return (_context.Branches?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
