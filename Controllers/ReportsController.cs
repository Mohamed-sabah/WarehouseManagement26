using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Data;
using WarehouseManagement.Models;
using WarehouseManagement.Models.ViewModels;

namespace WarehouseManagement.Controllers
{
    public class ReportsController : Controller
    {
        private readonly WarehouseContext _context;

        public ReportsController(WarehouseContext context)
        {
            _context = context;
        }

        // GET: Reports
        public IActionResult Index()
        {
            return View();
        }

        // GET: Reports/MaterialsReport
        public async Task<IActionResult> MaterialsReport(int? categoryId, string? searchTerm)
        {
            var query = _context.Materials
                .Include(m => m.Category)
                .Include(m => m.Stocks)
                    .ThenInclude(s => s.Location)
                .Include(m => m.Purchases)
                .Where(m => m.IsActive)
                .AsQueryable();

            if (categoryId.HasValue)
                query = query.Where(m => m.CategoryId == categoryId.Value);

            if (!string.IsNullOrEmpty(searchTerm))
                query = query.Where(m => m.Name.Contains(searchTerm) || m.Code.Contains(searchTerm));

            var materials = await query.OrderBy(m => m.Category!.Name).ThenBy(m => m.Name).ToListAsync();

            ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
            ViewBag.TotalMaterials = materials.Count;
            ViewBag.TotalQuantity = materials.Sum(m => m.TotalQuantity);
            ViewBag.TotalValue = materials.Sum(m => m.TotalValue);

            return View(materials);
        }

        // GET: Reports/LocationReport/5
        public async Task<IActionResult> LocationReport(int? id)
        {
            if (id == null)
            {
                ViewBag.Locations = await _context.Locations.Where(l => l.IsActive).OrderBy(l => l.Name).ToListAsync();
                return View("SelectLocation");
            }

            var location = await _context.Locations
                .Include(l => l.Stocks)
                    .ThenInclude(s => s.Material)
                        .ThenInclude(m => m.Category)
                .Include(l => l.Stocks)
                    .ThenInclude(s => s.Material)
                        .ThenInclude(m => m.Purchases)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (location == null) return NotFound();

            var viewModel = new LocationReportViewModel
            {
                Location = location,
                Stocks = location.Stocks.Where(s => s.Quantity > 0).ToList(),
                TotalItems = location.Stocks.Count(s => s.Quantity > 0),
                TotalQuantity = location.Stocks.Sum(s => s.Quantity),
                TotalValue = location.Stocks.Sum(s => s.CurrentValue)
            };

            return View(viewModel);
        }

        // GET: Reports/PurchasesYearly
        public async Task<IActionResult> PurchasesYearly(int? year)
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

            viewModel.ByMonth = purchases
                .GroupBy(p => p.PurchaseDate.Month)
                .ToDictionary(
                    g => g.Key,
                    g => new PurchaseMonthSummary
                    {
                        Month = g.Key,
                        Count = g.Count(),
                        TotalQuantity = g.Sum(p => p.Quantity),
                        TotalValue = g.Sum(p => p.TotalPrice)
                    });

            return View(viewModel);
        }

        // GET: Reports/PricingReport/5
        public async Task<IActionResult> PricingReport(int? id)
        {
            if (id == null)
            {
                ViewBag.Materials = await _context.Materials.Where(m => m.IsActive).OrderBy(m => m.Name).ToListAsync();
                return View("SelectMaterial");
            }

            var material = await _context.Materials
                .Include(m => m.Category)
                .Include(m => m.Stocks)
                    .ThenInclude(s => s.Location)
                .Include(m => m.Purchases.OrderBy(p => p.PurchaseDate))
                    .ThenInclude(p => p.Location)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (material == null) return NotFound();

            var viewModel = new PricingReportViewModel
            {
                Material = material,
                Purchases = material.Purchases.ToList(),
                TotalQuantityPurchased = material.Purchases.Sum(p => p.Quantity),
                TotalAmountSpent = material.Purchases.Sum(p => p.TotalPrice),
                AveragePrice = material.AveragePrice,
                CurrentValue = material.TotalValue,
                FirstPurchaseDate = material.Purchases.FirstOrDefault()?.PurchaseDate,
                LastPurchaseDate = material.Purchases.LastOrDefault()?.PurchaseDate
            };

            return View(viewModel);
        }

        // GET: Reports/InventoryForm2
        public async Task<IActionResult> InventoryForm2(int? year, int? locationId, string? department)
        {
            year ??= DateTime.Now.Year;

            var query = _context.InventoryRecords
                .Include(i => i.Material)
                    .ThenInclude(m => m.Category)
                .Include(i => i.Location)
                .Include(i => i.Consumptions)
                .Where(i => i.Year == year);

            if (locationId.HasValue)
                query = query.Where(i => i.LocationId == locationId.Value);

            if (!string.IsNullOrEmpty(department))
                query = query.Where(i => i.Department == department);

            var records = await query.OrderBy(i => i.Material.Category!.Name)
                                    .ThenBy(i => i.Material.Name)
                                    .ToListAsync();

            var viewModel = new InventoryReportViewModel
            {
                ReportDate = DateTime.Now,
                Year = year.Value,
                ReportTitle = $"نموذج رقم (2) - قائمة بموجودات المخازن لسنة {year}",
                Records = records,
                Statistics = new InventoryStatistics
                {
                    TotalItems = records.Count,
                    TotalQuantityByInventory = records.Sum(r => r.ActualQuantity),
                    TotalQuantityByRecords = records.Sum(r => r.RecordedQuantity),
                    TotalDifference = records.Sum(r => r.Difference),
                    TotalCost = records.Sum(r => r.TotalCost),
                    ItemsWithShortage = records.Count(r => r.HasShortage),
                    ItemsWithSurplus = records.Count(r => r.HasSurplus),
                    ItemsMatching = records.Count(r => r.IsMatching)
                }
            };

            if (locationId.HasValue)
            {
                var location = await _context.Locations.FindAsync(locationId.Value);
                viewModel.LocationName = location?.Name;
            }

            viewModel.Department = department;

            ViewBag.Locations = await _context.Locations.Where(l => l.IsActive).OrderBy(l => l.Name).ToListAsync();
            ViewBag.Departments = await _context.InventoryRecords.Select(i => i.Department).Distinct().Where(d => d != null).ToListAsync();
            ViewBag.AvailableYears = await _context.InventoryRecords.Select(i => i.Year).Distinct().OrderByDescending(y => y).ToListAsync();

            return View(viewModel);
        }

        // GET: Reports/ConsumptionForm5
        public async Task<IActionResult> ConsumptionForm5(int? year, string? department)
        {
            year ??= DateTime.Now.Year;

            var query = _context.ConsumptionRecords
                .Include(c => c.InventoryRecord)
                    .ThenInclude(i => i.Material)
                        .ThenInclude(m => m.Category)
                .Include(c => c.InventoryRecord)
                    .ThenInclude(i => i.Location)
                .Where(c => c.ReportDate.Year == year);

            if (!string.IsNullOrEmpty(department))
                query = query.Where(c => c.InventoryRecord.Department == department);

            var records = await query.OrderBy(c => c.InventoryRecord.Material.Name).ToListAsync();

            var viewModel = new ConsumptionReportViewModel
            {
                ReportDate = DateTime.Now,
                Department = department ?? "جميع الأقسام",
                ReportTitle = $"نموذج رقم (5) - قائمة بالموجودات المستهلكة لسنة {year}",
                Records = records,
                Statistics = new ConsumptionStatistics
                {
                    TotalItems = records.Count,
                    TotalQuantity = records.Sum(c => c.ConsumedQuantity),
                    TotalOriginalValue = records.Sum(c => c.StoredOriginalValue),
                    TotalResidualValue = records.Sum(c => c.StoredResidualValue),
                    ItemsDisposed = records.Count(c => c.IsDisposed),
                    ItemsPending = records.Count(c => !c.IsDisposed),
                    ByReason = records.GroupBy(c => c.Reason).ToDictionary(g => g.Key, g => g.Count()),
                    ByDecision = records.GroupBy(c => c.Decision).ToDictionary(g => g.Key, g => g.Count())
                }
            };

            ViewBag.Year = year;
            ViewBag.Departments = await _context.InventoryRecords.Select(i => i.Department).Distinct().Where(d => d != null).ToListAsync();
            ViewBag.AvailableYears = await _context.ConsumptionRecords.Select(c => c.ReportDate.Year).Distinct().OrderByDescending(y => y).ToListAsync();

            return View(viewModel);
        }

        // GET: Reports/LowStockReport
        public async Task<IActionResult> LowStockReport()
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

            ViewBag.TotalItems = lowStockItems.Count;
            ViewBag.CriticalCount = lowStockItems.Count(s => s.Quantity == 0);
            ViewBag.WarningCount = lowStockItems.Count(s => s.Quantity > 0);

            return View(lowStockItems);
        }

        // GET: Reports/ExpiryReport
        public async Task<IActionResult> ExpiryReport(int days = 30)
        {
            var expiryDate = DateTime.Now.AddDays(days);

            var stocks = await _context.MaterialStocks
                .Include(s => s.Material)
                    .ThenInclude(m => m.Category)
                .Include(s => s.Location)
                .Where(s => s.ExpiryDate.HasValue && s.Quantity > 0)
                .OrderBy(s => s.ExpiryDate)
                .ToListAsync();

            ViewBag.Days = days;
            ViewBag.ExpiredCount = stocks.Count(s => s.IsExpired);
            ViewBag.ExpiringSoonCount = stocks.Count(s => s.ExpiryDate <= expiryDate && !s.IsExpired);
            ViewBag.TotalAtRisk = stocks.Count(s => s.ExpiryDate <= expiryDate);

            return View(stocks);
        }
    }
}
