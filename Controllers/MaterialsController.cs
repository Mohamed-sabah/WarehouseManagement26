using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Data;
using WarehouseManagement.Models;
using WarehouseManagement.Models.ViewModels;

namespace WarehouseManagement.Controllers
{
    /// <summary>
    /// Controller للمواد - نقطة الإدخال الوحيدة للمواد
    /// </summary>
    public class MaterialsController : Controller
    {
        private readonly WarehouseContext _context;

        public MaterialsController(WarehouseContext context)
        {
            _context = context;
        }

        // GET: Materials
        public async Task<IActionResult> Index(string? searchTerm, int? categoryId, bool lowStockOnly = false)
        {
            var query = _context.Materials
                .Include(m => m.Category)
                .Include(m => m.Stocks)
                    .ThenInclude(s => s.Location)
                .Include(m => m.Purchases)
                .Where(m => m.IsActive)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(m => m.Name.Contains(searchTerm) || 
                                        m.Code.Contains(searchTerm));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(m => m.CategoryId == categoryId.Value);
            }

            var materials = await query.OrderBy(m => m.Name).ToListAsync();

            if (lowStockOnly)
            {
                materials = materials.Where(m => m.IsLowStock).ToList();
            }

            ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
            ViewBag.SearchTerm = searchTerm;
            ViewBag.CategoryId = categoryId;
            ViewBag.LowStockOnly = lowStockOnly;

            return View(materials);
        }

        // GET: Materials/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var material = await _context.Materials
                .Include(m => m.Category)
                .Include(m => m.Stocks)
                    .ThenInclude(s => s.Location)
                .Include(m => m.Purchases.OrderByDescending(p => p.PurchaseDate).Take(10))
                    .ThenInclude(p => p.Location)
                .Include(m => m.InventoryRecords.OrderByDescending(i => i.Year).Take(5))
                .Include(m => m.Transfers.OrderByDescending(t => t.TransferDate).Take(10))
                .FirstOrDefaultAsync(m => m.Id == id);

            if (material == null) return NotFound();

            var viewModel = new MaterialDetailsViewModel
            {
                Material = material,
                Stocks = material.Stocks.ToList(),
                RecentPurchases = material.Purchases.ToList(),
                RecentInventories = material.InventoryRecords.ToList(),
                RecentTransfers = material.Transfers.ToList(),
                TotalQuantity = material.TotalQuantity,
                TotalValue = material.TotalValue,
                LocationsCount = material.Stocks.Count,
                PurchasesCount = await _context.Purchases.CountAsync(p => p.MaterialId == id)
            };

            return View(viewModel);
        }

        // GET: Materials/Create
        public async Task<IActionResult> Create()
        {
            var viewModel = new MaterialCreateEditViewModel
            {
                Material = new Material { Unit = "قطعة" },
                Categories = await _context.Categories.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync(),
                Locations = await _context.Locations.Where(l => l.IsActive).OrderBy(l => l.Name).ToListAsync()
            };
            return View(viewModel);
        }

        // POST: Materials/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MaterialCreateEditViewModel viewModel)
        {
            // إزالة التحقق من الحقول الفرعية إذا لم يتم اختيار إضافة مخزون أولي
            if (!viewModel.AddInitialStock)
            {
                ModelState.Remove("InitialStock.LocationId");
                ModelState.Remove("InitialStock.Quantity");
            }

            if (ModelState.IsValid)
            {
                // التحقق من أن الرمز فريد
                if (await _context.Materials.AnyAsync(m => m.Code == viewModel.Material.Code))
                {
                    ModelState.AddModelError("Material.Code", "رمز المادة موجود مسبقاً");
                    await LoadSelectLists(viewModel);
                    return View(viewModel);
                }

                viewModel.Material.CreatedDate = DateTime.Now;
                viewModel.Material.LastUpdated = DateTime.Now;
                _context.Materials.Add(viewModel.Material);
                await _context.SaveChangesAsync();

                // إضافة مخزون أولي إذا طلب
                if (viewModel.AddInitialStock && viewModel.InitialStock != null && viewModel.InitialStock.LocationId > 0)
                {
                    var stock = new MaterialStock
                    {
                        MaterialId = viewModel.Material.Id,
                        LocationId = viewModel.InitialStock.LocationId,
                        Quantity = viewModel.InitialStock.Quantity,
                        ExpiryDate = viewModel.InitialStock.ExpiryDate,
                        Condition = "جيدة",
                        CreatedDate = DateTime.Now,
                        LastUpdated = DateTime.Now
                    };
                    _context.MaterialStocks.Add(stock);

                    // إضافة سجل شراء إذا كان هناك سعر
                    if (viewModel.InitialStock.UnitPrice.HasValue && viewModel.InitialStock.UnitPrice > 0)
                    {
                        var purchase = new Purchase
                        {
                            MaterialId = viewModel.Material.Id,
                            LocationId = viewModel.InitialStock.LocationId,
                            Quantity = viewModel.InitialStock.Quantity,
                            UnitPrice = viewModel.InitialStock.UnitPrice.Value,
                            StoredTotalPrice = viewModel.InitialStock.Quantity * viewModel.InitialStock.UnitPrice.Value,
                            PurchaseDate = DateTime.Now,
                            Method = AcquisitionMethod.OpeningBalance,
                            IsAddedToStock = true,
                            AddedToStockDate = DateTime.Now,
                            Notes = "رصيد افتتاحي",
                            CreatedDate = DateTime.Now
                        };
                        _context.Purchases.Add(purchase);
                    }

                    await _context.SaveChangesAsync();
                }

                TempData["Success"] = "تم إضافة المادة بنجاح";
                return RedirectToAction(nameof(Details), new { id = viewModel.Material.Id });
            }

            await LoadSelectLists(viewModel);
            return View(viewModel);
        }

        // GET: Materials/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var material = await _context.Materials.FindAsync(id);
            if (material == null) return NotFound();

            var viewModel = new MaterialCreateEditViewModel
            {
                Material = material,
                Categories = await _context.Categories.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync(),
                Locations = await _context.Locations.Where(l => l.IsActive).OrderBy(l => l.Name).ToListAsync()
            };

            return View(viewModel);
        }

        // POST: Materials/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MaterialCreateEditViewModel viewModel)
        {
            if (id != viewModel.Material.Id) return NotFound();

            // إزالة التحقق من الحقول الفرعية
            ModelState.Remove("InitialStock.LocationId");
            ModelState.Remove("InitialStock.Quantity");

            if (ModelState.IsValid)
            {
                // التحقق من أن الرمز فريد
                if (await _context.Materials.AnyAsync(m => m.Code == viewModel.Material.Code && m.Id != id))
                {
                    ModelState.AddModelError("Material.Code", "رمز المادة موجود مسبقاً");
                    await LoadSelectLists(viewModel);
                    return View(viewModel);
                }

                try
                {
                    viewModel.Material.LastUpdated = DateTime.Now;
                    _context.Update(viewModel.Material);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "تم تحديث المادة بنجاح";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Materials.AnyAsync(m => m.Id == id))
                        return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Details), new { id });
            }

            await LoadSelectLists(viewModel);
            return View(viewModel);
        }

        // GET: Materials/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var material = await _context.Materials
                .Include(m => m.Category)
                .Include(m => m.Stocks)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (material == null) return NotFound();

            return View(material);
        }

        // POST: Materials/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var material = await _context.Materials
                .Include(m => m.Stocks)
                .Include(m => m.Purchases)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (material == null) return NotFound();

            // التحقق من عدم وجود عمليات مرتبطة
            if (material.Stocks.Any(s => s.Quantity > 0))
            {
                TempData["Error"] = "لا يمكن حذف المادة لأنها تحتوي على مخزون";
                return RedirectToAction(nameof(Delete), new { id });
            }

            // Soft delete - فقط إلغاء التفعيل
            material.IsActive = false;
            material.LastUpdated = DateTime.Now;
            await _context.SaveChangesAsync();
            
            TempData["Success"] = "تم حذف المادة بنجاح";
            return RedirectToAction(nameof(Index));
        }

        private async Task LoadSelectLists(MaterialCreateEditViewModel viewModel)
        {
            viewModel.Categories = await _context.Categories.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
            viewModel.Locations = await _context.Locations.Where(l => l.IsActive).OrderBy(l => l.Name).ToListAsync();
        }
    }
}
