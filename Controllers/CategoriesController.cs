using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Data;
using WarehouseManagement.Models;

namespace WarehouseManagement.Controllers
{
    [Authorize]
    public class CategoriesController : Controller
    {
        private readonly WarehouseContext _context;

        public CategoriesController(WarehouseContext context)
        {
            _context = context;
        }

        // GET: Categories
        public async Task<IActionResult> Index()
        {
            var categories = await _context.Categories
                .Include(c => c.Materials)
                .Include(c => c.ParentCategory)
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View(categories);
        }

        // GET: Categories/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var category = await _context.Categories
                .Include(c => c.Materials.Where(m => m.IsActive))
                    .ThenInclude(m => m.Stocks)
                .Include(c => c.SubCategories)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null) return NotFound();

            return View(category);
        }

        // GET: Categories/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.ParentCategories = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View();
        }

        // POST: Categories/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description,Code,ParentCategoryId")] Category category)
        {
            if (ModelState.IsValid)
            {
                category.CreatedDate = DateTime.Now;
                _context.Categories.Add(category);
                await _context.SaveChangesAsync();
                TempData["Success"] = "تم إضافة الفئة بنجاح";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.ParentCategories = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View(category);
        }

        // GET: Categories/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();

            ViewBag.ParentCategories = await _context.Categories
                .Where(c => c.IsActive && c.Id != id)
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View(category);
        }

        // POST: Categories/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,Code,ParentCategoryId,IsActive,CreatedDate")] Category category)
        {
            if (id != category.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(category);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "تم تحديث الفئة بنجاح";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Categories.AnyAsync(c => c.Id == id))
                        return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewBag.ParentCategories = await _context.Categories
                .Where(c => c.IsActive && c.Id != id)
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View(category);
        }

        // GET: Categories/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var category = await _context.Categories
                .Include(c => c.Materials)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null) return NotFound();

            return View(category);
        }

        // POST: Categories/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Materials)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null) return NotFound();

            if (category.Materials.Any(m => m.IsActive))
            {
                TempData["Error"] = "لا يمكن حذف الفئة لأنها تحتوي على مواد";
                return RedirectToAction(nameof(Delete), new { id });
            }

            // Soft delete
            category.IsActive = false;
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم حذف الفئة بنجاح";
            return RedirectToAction(nameof(Index));
        }
    }
}
