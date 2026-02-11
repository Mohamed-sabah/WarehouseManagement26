using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Data;
using WarehouseManagement.Models;
using WarehouseManagement.Models.ViewModels;

namespace WarehouseManagement.Controllers
{
    [Authorize]
    public class PurchasesController : Controller
    {
        private readonly WarehouseContext _context;

        public PurchasesController(WarehouseContext context)
        {
            _context = context;
        }

        // GET: Purchases
        public async Task<IActionResult> Index(int? materialId, int? locationId, int? year, AcquisitionMethod? method)
        {
            var query = _context.Purchases
                .Include(p => p.Material)
                    .ThenInclude(m => m.Category)
                .Include(p => p.Location)
                .AsQueryable();

            if (materialId.HasValue)
                query = query.Where(p => p.MaterialId == materialId.Value);

            if (locationId.HasValue)
                query = query.Where(p => p.LocationId == locationId.Value);

            if (year.HasValue)
                query = query.Where(p => p.PurchaseDate.Year == year.Value);

            if (method.HasValue)
                query = query.Where(p => p.Method == method.Value);

            var purchases = await query.OrderByDescending(p => p.PurchaseDate).ToListAsync();

            var viewModel = new PurchaseListViewModel
            {
                MaterialId = materialId,
                LocationId = locationId,
                Year = year,
                Method = method,
                Purchases = purchases,
                TotalPurchases = purchases.Count,
                TotalQuantity = purchases.Sum(p => p.Quantity),
                TotalValue = purchases.Sum(p => p.TotalPrice),
                Materials = await _context.Materials.Where(m => m.IsActive).OrderBy(m => m.Name).ToListAsync(),
                Locations = await _context.Locations.Where(l => l.IsActive).OrderBy(l => l.Name).ToListAsync(),
                AvailableYears = await _context.Purchases.Select(p => p.PurchaseDate.Year).Distinct().OrderByDescending(y => y).ToListAsync()
            };

            return View(viewModel);
        }

        // GET: Purchases/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var purchase = await _context.Purchases
                .Include(p => p.Material)
                    .ThenInclude(m => m.Category)
                .Include(p => p.Location)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (purchase == null) return NotFound();

            return View(purchase);
        }

        // GET: Purchases/Create
        public async Task<IActionResult> Create(int? materialId)
        {
            var viewModel = new PurchaseCreateEditViewModel
            {
                Purchase = new Purchase
                {
                    PurchaseDate = DateTime.Now,
                    Method = AcquisitionMethod.Purchase,
                    Currency = "IQD",
                    ExchangeRate = 1
                },
                Materials = await _context.Materials.Where(m => m.IsActive).OrderBy(m => m.Name).ToListAsync(),
                Locations = await _context.Locations.Where(l => l.IsActive).OrderBy(l => l.Name).ToListAsync()
            };

            if (materialId.HasValue)
            {
                viewModel.Purchase.MaterialId = materialId.Value;
            }

            return View(viewModel);
        }

        // POST: Purchases/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PurchaseCreateEditViewModel viewModel)
        {
            // إزالة التحقق من المادة الجديدة إذا لم يتم اختيارها
            if (!viewModel.CreateNewMaterial)
            {
                ModelState.Remove("NewMaterial.Name");
                ModelState.Remove("NewMaterial.Code");
                ModelState.Remove("NewMaterial.CategoryId");
                ModelState.Remove("NewMaterial.Unit");
            }

            //if (ModelState.IsValid)
            //{
                // إنشاء مادة جديدة إذا طلب
                if (viewModel.CreateNewMaterial && viewModel.NewMaterial != null)
                {
                    if (await _context.Materials.AnyAsync(m => m.Code == viewModel.NewMaterial.Code))
                    {
                        ModelState.AddModelError("NewMaterial.Code", "رمز المادة موجود مسبقاً");
                        await LoadSelectLists(viewModel);
                        return View(viewModel);
                    }

                    var newMaterial = new Material
                    {
                        Name = viewModel.NewMaterial.Name,
                        Code = viewModel.NewMaterial.Code,
                        Description = viewModel.NewMaterial.Description,
                        CategoryId = viewModel.NewMaterial.CategoryId,
                        Unit = viewModel.NewMaterial.Unit,
                        CreatedDate = DateTime.Now,
                        LastUpdated = DateTime.Now
                    };
                    _context.Materials.Add(newMaterial);
                    await _context.SaveChangesAsync();

                    viewModel.Purchase.MaterialId = newMaterial.Id;
                }

                viewModel.Purchase.StoredTotalPrice = viewModel.Purchase.TotalPrice;
                viewModel.Purchase.CreatedDate = DateTime.Now;
                _context.Purchases.Add(viewModel.Purchase);

                if (viewModel.AddToStockAutomatically)
                {
                    await AddToStock(viewModel.Purchase);
                    viewModel.Purchase.IsAddedToStock = true;
                    viewModel.Purchase.AddedToStockDate = DateTime.Now;
                }

                await _context.SaveChangesAsync();
                TempData["Success"] = "تم تسجيل عملية الشراء بنجاح";
                return RedirectToAction(nameof(Index));
            //}

            await LoadSelectLists(viewModel);
            return View(viewModel);
        }

        // POST: Purchases/AddToStock/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToStock(int id)
        {
            var purchase = await _context.Purchases.FindAsync(id);
            if (purchase == null) return NotFound();

            if (purchase.IsAddedToStock)
            {
                TempData["Warning"] = "تم إضافة هذه الكمية للمخزون مسبقاً";
                return RedirectToAction(nameof(Details), new { id });
            }

            await AddToStock(purchase);
            purchase.IsAddedToStock = true;
            purchase.AddedToStockDate = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم إضافة الكمية للمخزون بنجاح";
            return RedirectToAction(nameof(Details), new { id });
        }

        private async Task AddToStock(Purchase purchase)
        {
            var existingStock = await _context.MaterialStocks
                .FirstOrDefaultAsync(s => s.MaterialId == purchase.MaterialId && 
                                         s.LocationId == purchase.LocationId);

            if (existingStock != null)
            {
                existingStock.Quantity += purchase.Quantity;
                existingStock.LastUpdated = DateTime.Now;
                if (purchase.ExpiryDate.HasValue)
                    existingStock.ExpiryDate = purchase.ExpiryDate;
            }
            else
            {
                var newStock = new MaterialStock
                {
                    MaterialId = purchase.MaterialId,
                    LocationId = purchase.LocationId,
                    Quantity = purchase.Quantity,
                    ExpiryDate = purchase.ExpiryDate,
                    BatchNumber = purchase.BatchNumber,
                    Condition = "جيدة",
                    CreatedDate = DateTime.Now,
                    LastUpdated = DateTime.Now
                };
                _context.MaterialStocks.Add(newStock);
            }
        }

        // GET: Purchases/YearlyReport
        public async Task<IActionResult> YearlyReport(int? year)
        {
            year ??= DateTime.Now.Year;

            var purchases = await _context.Purchases
                .Include(p => p.Material)
                    .ThenInclude(m => m.Category)
                .Include(p => p.Location)
                .Where(p => p.PurchaseDate.Year == year)
                .OrderBy(p => p.PurchaseDate)
                .ToListAsync();

            var viewModel = new PurchaseYearlyReportViewModel
            {
                Year = year.Value,
                Purchases = purchases,
                TotalPurchases = purchases.Count,
                TotalQuantity = purchases.Sum(p => p.Quantity),
                TotalValue = purchases.Sum(p => p.TotalPrice),
                UniqueMaterials = purchases.Select(p => p.MaterialId).Distinct().Count(),
                AvailableYears = await _context.Purchases.Select(p => p.PurchaseDate.Year).Distinct().OrderByDescending(y => y).ToListAsync()
            };

            viewModel.ByCategory = purchases
                .Where(p => p.Material.Category != null)
                .GroupBy(p => p.Material.Category!.Name)
                .ToDictionary(
                    g => g.Key,
                    g => new PurchaseCategorySummary
                    {
                        CategoryName = g.Key,
                        Count = g.Count(),
                        TotalQuantity = g.Sum(p => p.Quantity),
                        TotalValue = g.Sum(p => p.TotalPrice)
                    });

            return View(viewModel);
        }

        // GET: Purchases/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var purchase = await _context.Purchases
                .Include(p => p.Material)
                .Include(p => p.Location)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (purchase == null) return NotFound();

            return View(purchase);
        }

        // POST: Purchases/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var purchase = await _context.Purchases.FindAsync(id);
            if (purchase == null) return NotFound();

            if (purchase.IsAddedToStock)
            {
                TempData["Error"] = "لا يمكن حذف عملية شراء تم إضافتها للمخزون";
                return RedirectToAction(nameof(Delete), new { id });
            }

            _context.Purchases.Remove(purchase);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم حذف سجل الشراء بنجاح";
            return RedirectToAction(nameof(Index));
        }

        private async Task LoadSelectLists(PurchaseCreateEditViewModel viewModel)
        {
            viewModel.Materials = await _context.Materials.Where(m => m.IsActive).OrderBy(m => m.Name).ToListAsync();
            viewModel.Locations = await _context.Locations.Where(l => l.IsActive).OrderBy(l => l.Name).ToListAsync();
        }
    }
}
