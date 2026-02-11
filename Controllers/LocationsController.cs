using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Data;
using WarehouseManagement.Models;

namespace WarehouseManagement.Controllers
{
    [Authorize]
    public class LocationsController : Controller
    {
        private readonly WarehouseContext _context;

        public LocationsController(WarehouseContext context)
        {
            _context = context;
        }

        // GET: Locations
        public async Task<IActionResult> Index()
        {
            var locations = await _context.Locations
                .Include(l => l.Stocks)
                .Where(l => l.IsActive)
                .OrderBy(l => l.Name)
                .ToListAsync();

            return View(locations);
        }

        // GET: Locations/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var location = await _context.Locations
                .Include(l => l.Stocks)
                    .ThenInclude(s => s.Material)
                        .ThenInclude(m => m.Category)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (location == null) return NotFound();

            ViewBag.TotalMaterials = location.Stocks.Count;
            ViewBag.TotalQuantity = location.Stocks.Sum(s => s.Quantity);
            ViewBag.TotalValue = location.Stocks.Sum(s => s.CurrentValue);

            return View(location);
        }

        // GET: Locations/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Locations/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Code,Description,Type,MaxCapacity,Floor,Building,Address,ResponsibleDepartment,ResponsiblePerson")] Location location)
        {
            if (ModelState.IsValid)
            {
                if (await _context.Locations.AnyAsync(l => l.Code == location.Code))
                {
                    ModelState.AddModelError("Code", "رمز الموقع موجود مسبقاً");
                    return View(location);
                }

                location.CreatedDate = DateTime.Now;
                _context.Locations.Add(location);
                await _context.SaveChangesAsync();
                TempData["Success"] = "تم إضافة الموقع بنجاح";
                return RedirectToAction(nameof(Index));
            }
            return View(location);
        }

        // GET: Locations/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var location = await _context.Locations.FindAsync(id);
            if (location == null) return NotFound();

            return View(location);
        }

        // POST: Locations/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Code,Description,Type,MaxCapacity,Floor,Building,Address,ResponsibleDepartment,ResponsiblePerson,IsActive,CreatedDate")] Location location)
        {
            if (id != location.Id) return NotFound();

            if (ModelState.IsValid)
            {
                if (await _context.Locations.AnyAsync(l => l.Code == location.Code && l.Id != id))
                {
                    ModelState.AddModelError("Code", "رمز الموقع موجود مسبقاً");
                    return View(location);
                }

                try
                {
                    _context.Update(location);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "تم تحديث الموقع بنجاح";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Locations.AnyAsync(l => l.Id == id))
                        return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(location);
        }

        // GET: Locations/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var location = await _context.Locations
                .Include(l => l.Stocks)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (location == null) return NotFound();

            return View(location);
        }

        // POST: Locations/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var location = await _context.Locations
                .Include(l => l.Stocks)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (location == null) return NotFound();

            if (location.Stocks.Any(s => s.Quantity > 0))
            {
                TempData["Error"] = "لا يمكن حذف الموقع لأنه يحتوي على مواد";
                return RedirectToAction(nameof(Delete), new { id });
            }

            location.IsActive = false;
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم حذف الموقع بنجاح";
            return RedirectToAction(nameof(Index));
        }
    }
}
