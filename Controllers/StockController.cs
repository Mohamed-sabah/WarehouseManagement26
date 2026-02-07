using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Data;
using WarehouseManagement.Models;
using WarehouseManagement.Models.ViewModels;

namespace WarehouseManagement.Controllers
{
    public class StockController : Controller
    {
        private readonly WarehouseContext _context;

        public StockController(WarehouseContext context)
        {
            _context = context;
        }

        // GET: Stock
        public async Task<IActionResult> Index(int? locationId, int? categoryId, 
            string? searchTerm, bool lowStockOnly = false, bool expiringOnly = false)
        {
            var query = _context.MaterialStocks
                .Include(s => s.Material)
                    .ThenInclude(m => m.Category)
                .Include(s => s.Location)
                .Where(s => s.Material.IsActive)
                .AsQueryable();

            if (locationId.HasValue)
                query = query.Where(s => s.LocationId == locationId.Value);

            if (categoryId.HasValue)
                query = query.Where(s => s.Material.CategoryId == categoryId.Value);

            if (!string.IsNullOrEmpty(searchTerm))
                query = query.Where(s => s.Material.Name.Contains(searchTerm) ||
                                        s.Material.Code.Contains(searchTerm));

            var stocks = await query.OrderBy(s => s.Location.Name)
                                   .ThenBy(s => s.Material.Name)
                                   .ToListAsync();

            if (lowStockOnly)
                stocks = stocks.Where(s => s.Quantity <= s.Material.MinimumStock).ToList();

            if (expiringOnly)
                stocks = stocks.Where(s => s.IsExpiringSoon || s.IsExpired).ToList();

            ViewBag.TotalItems = stocks.Count;
            ViewBag.TotalQuantity = stocks.Sum(s => s.Quantity);
            ViewBag.TotalValue = stocks.Sum(s => s.CurrentValue);
            ViewBag.LowStockCount = stocks.Count(s => s.Quantity <= s.Material.MinimumStock);
            ViewBag.ExpiringCount = stocks.Count(s => s.IsExpiringSoon);
            ViewBag.ExpiredCount = stocks.Count(s => s.IsExpired);

            ViewBag.Locations = await _context.Locations.Where(l => l.IsActive).OrderBy(l => l.Name).ToListAsync();
            ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
            ViewBag.LocationId = locationId;
            ViewBag.CategoryId = categoryId;
            ViewBag.SearchTerm = searchTerm;
            ViewBag.LowStockOnly = lowStockOnly;
            ViewBag.ExpiringOnly = expiringOnly;

            return View(stocks);
        }

        // GET: Stock/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var stock = await _context.MaterialStocks
                .Include(s => s.Material)
                    .ThenInclude(m => m.Category)
                .Include(s => s.Material)
                    .ThenInclude(m => m.Purchases.OrderByDescending(p => p.PurchaseDate).Take(10))
                .Include(s => s.Location)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (stock == null) return NotFound();

            ViewBag.TransfersIn = await _context.Transfers
                .Include(t => t.FromLocation)
                .Where(t => t.MaterialId == stock.MaterialId && 
                           t.ToLocationId == stock.LocationId && 
                           t.IsExecuted)
                .OrderByDescending(t => t.TransferDate)
                .Take(10)
                .ToListAsync();

            ViewBag.TransfersOut = await _context.Transfers
                .Include(t => t.ToLocation)
                .Where(t => t.MaterialId == stock.MaterialId && 
                           t.FromLocationId == stock.LocationId && 
                           t.IsExecuted)
                .OrderByDescending(t => t.TransferDate)
                .Take(10)
                .ToListAsync();

            return View(stock);
        }

        // GET: Stock/Create - إضافة هذا الـ action المفقود
        public async Task<IActionResult> Create()
        {
            var viewModel = new StockCreateEditViewModel
            {
                Stock = new MaterialStock
                {
                    Condition = "جيدة",
                    Quantity = 0
                },
                Materials = await _context.Materials.Where(m => m.IsActive).OrderBy(m => m.Name).ToListAsync(),
                Locations = await _context.Locations.Where(l => l.IsActive).OrderBy(l => l.Name).ToListAsync()
            };

            return View(viewModel);
        }

        // POST: Stock/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(StockCreateEditViewModel viewModel)
        {
            //if (ModelState.IsValid)
            //{
                // التحقق من عدم وجود سجل مسبق
                var existingStock = await _context.MaterialStocks
                    .FirstOrDefaultAsync(s => s.MaterialId == viewModel.Stock.MaterialId && 
                                             s.LocationId == viewModel.Stock.LocationId);

                if (existingStock != null)
                {
                    ModelState.AddModelError("", "يوجد سجل مخزون لهذه المادة في هذا الموقع");
                    viewModel.Materials = await _context.Materials.Where(m => m.IsActive).OrderBy(m => m.Name).ToListAsync();
                    viewModel.Locations = await _context.Locations.Where(l => l.IsActive).OrderBy(l => l.Name).ToListAsync();
                    return View(viewModel);
                }

                viewModel.Stock.CreatedDate = DateTime.Now;
                viewModel.Stock.LastUpdated = DateTime.Now;

                _context.MaterialStocks.Add(viewModel.Stock);
                await _context.SaveChangesAsync();

                TempData["Success"] = "تم إضافة سجل المخزون بنجاح";
                return RedirectToAction(nameof(Details), new { id = viewModel.Stock.Id });
            //}

            viewModel.Materials = await _context.Materials.Where(m => m.IsActive).OrderBy(m => m.Name).ToListAsync();
            viewModel.Locations = await _context.Locations.Where(l => l.IsActive).OrderBy(l => l.Name).ToListAsync();
            return View(viewModel);
        }

        // POST: Stock/CreateAjax (AJAX) - الاحتفاظ بالنسخة القديمة للتوافق
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAjax(int materialId, int locationId, int quantity, 
            DateTime? expiryDate, string? batchNumber, string? condition, string? exactLocation)
        {
            var existingStock = await _context.MaterialStocks
                .FirstOrDefaultAsync(s => s.MaterialId == materialId && s.LocationId == locationId);

            if (existingStock != null)
            {
                return Json(new { success = false, message = "يوجد سجل مخزون لهذه المادة في هذا الموقع" });
            }

            var stock = new MaterialStock
            {
                MaterialId = materialId,
                LocationId = locationId,
                Quantity = quantity,
                ExpiryDate = expiryDate,
                BatchNumber = batchNumber,
                Condition = condition ?? "جيدة",
                ExactLocation = exactLocation,
                CreatedDate = DateTime.Now,
                LastUpdated = DateTime.Now
            };

            _context.MaterialStocks.Add(stock);
            await _context.SaveChangesAsync();

            return Json(new { success = true, stockId = stock.Id });
        }

        // GET: Stock/Adjust/5
        public async Task<IActionResult> Adjust(int? id)
        {
            if (id == null) return NotFound();

            var stock = await _context.MaterialStocks
                .Include(s => s.Material)
                .Include(s => s.Location)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (stock == null) return NotFound();

            var viewModel = new StockAdjustmentViewModel
            {
                StockId = stock.Id,
                MaterialName = stock.Material.Name,
                LocationName = stock.Location.Name,
                CurrentQuantity = stock.Quantity,
                AdjustmentType = StockAdjustmentType.Add
            };

            return View(viewModel);
        }

        // POST: Stock/Adjust
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Adjust(StockAdjustmentViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var stock = await _context.MaterialStocks.FindAsync(viewModel.StockId);
                if (stock == null) return NotFound();

                int newQuantity = viewModel.AdjustmentType switch
                {
                    StockAdjustmentType.Add => stock.Quantity + viewModel.Quantity,
                    StockAdjustmentType.Deduct => stock.Quantity - viewModel.Quantity,
                    StockAdjustmentType.Set => viewModel.Quantity,
                    _ => stock.Quantity
                };

                if (newQuantity < 0)
                {
                    ModelState.AddModelError("Quantity", "الكمية الناتجة لا يمكن أن تكون سالبة");
                    return View(viewModel);
                }

                stock.Quantity = newQuantity;
                stock.LastUpdated = DateTime.Now;
                stock.Notes = $"{stock.Notes}\n[{DateTime.Now:yyyy-MM-dd}] تعديل: {viewModel.AdjustmentType} {viewModel.Quantity} - {viewModel.Reason}";

                await _context.SaveChangesAsync();

                TempData["Success"] = "تم تعديل المخزون بنجاح";
                return RedirectToAction(nameof(Details), new { id = viewModel.StockId });
            }

            return View(viewModel);
        }

        // GET: Stock/LowStock
        public async Task<IActionResult> LowStock()
        {
            var stocks = await _context.MaterialStocks
                .Include(s => s.Material)
                    .ThenInclude(m => m.Category)
                .Include(s => s.Location)
                .Where(s => s.Material.IsActive)
                .ToListAsync();

            var lowStockItems = stocks.Where(s => s.Quantity <= s.Material.MinimumStock)
                                     .OrderBy(s => (double)s.Quantity / Math.Max(1, s.Material.MinimumStock))
                                     .ToList();

            return View(lowStockItems);
        }

        // GET: Stock/Expiring
        public async Task<IActionResult> Expiring(int days = 30)
        {
            var expiryDate = DateTime.Now.AddDays(days);

            var stocks = await _context.MaterialStocks
                .Include(s => s.Material)
                    .ThenInclude(m => m.Category)
                .Include(s => s.Location)
                .Where(s => s.ExpiryDate.HasValue && 
                           s.ExpiryDate <= expiryDate &&
                           s.Quantity > 0)
                .OrderBy(s => s.ExpiryDate)
                .ToListAsync();

            ViewBag.Days = days;
            ViewBag.ExpiredCount = stocks.Count(s => s.IsExpired);
            ViewBag.ExpiringSoonCount = stocks.Count(s => s.IsExpiringSoon && !s.IsExpired);

            return View(stocks);
        }

        // GET: Stock/ByLocation/5
        public async Task<IActionResult> ByLocation(int? id)
        {
            if (id == null) return NotFound();

            var location = await _context.Locations.FindAsync(id);
            if (location == null) return NotFound();

            var stocks = await _context.MaterialStocks
                .Include(s => s.Material)
                    .ThenInclude(m => m.Category)
                .Include(s => s.Material)
                    .ThenInclude(m => m.Purchases)
                .Where(s => s.LocationId == id && s.Quantity > 0)
                .OrderBy(s => s.Material.Category!.Name)
                .ThenBy(s => s.Material.Name)
                .ToListAsync();

            ViewBag.Location = location;
            ViewBag.TotalItems = stocks.Count;
            ViewBag.TotalQuantity = stocks.Sum(s => s.Quantity);
            ViewBag.TotalValue = stocks.Sum(s => s.CurrentValue);

            return View(stocks);
        }

        // GET: Stock/LocationReport - إضافة تقرير الموقع
        public async Task<IActionResult> LocationReport(int? id)
        {
            if (id == null)
            {
                // عرض قائمة المواقع للاختيار
                var locations = await _context.Locations
                    .Include(l => l.Stocks)
                        .ThenInclude(s => s.Material)
                    .Where(l => l.IsActive)
                    .ToListAsync();

                var viewModel = new LocationReportViewModel
                {
                    Locations = locations.Select(l => new LocationSummaryItemViewModel
                    {
                        LocationId = l.Id,
                        LocationName = l.Name,
                        LocationCode = l.Code,
                        ManagerName = l.ResponsiblePerson,
                        MaterialsCount = l.Stocks.Count(s => s.Quantity > 0),
                        TotalQuantity = l.Stocks.Sum(s => s.Quantity),
                        TotalValue = l.Stocks.Sum(s => s.CurrentValue),
                        LowStockCount = l.Stocks.Count(s => s.Quantity <= s.Material.MinimumStock),
                        IsActive = l.IsActive
                    }).ToList()
                };

                return View(viewModel);
            }

            // عرض تفاصيل موقع محدد
            var location = await _context.Locations
                .Include(l => l.Stocks)
                    .ThenInclude(s => s.Material)
                        .ThenInclude(m => m.Category)
                .Include(l => l.Stocks)
                    .ThenInclude(s => s.Material)
                        .ThenInclude(m => m.Purchases)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (location == null) return NotFound();

            var detailViewModel = new LocationReportViewModel
            {
                Location = location,
                Stocks = location.Stocks.Where(s => s.Quantity > 0).ToList(),
                TotalItems = location.Stocks.Count(s => s.Quantity > 0),
                TotalQuantity = location.Stocks.Sum(s => s.Quantity),
                TotalValue = location.Stocks.Sum(s => s.CurrentValue)
            };

            return View("LocationReportDetail", detailViewModel);
        }
    }
}
