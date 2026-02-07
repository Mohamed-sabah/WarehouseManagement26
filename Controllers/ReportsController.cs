using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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

        #region Materials Reports

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

        // GET: Reports/MaterialsValue
        public async Task<IActionResult> MaterialsValue(int? categoryId, int? locationId, string sortBy = "value")
        {
            var query = _context.MaterialStocks
                .Include(s => s.Material)
                    .ThenInclude(m => m.Category)
                .Include(s => s.Material)
                    .ThenInclude(m => m.Purchases)
                .Include(s => s.Location)
                .Where(s => s.Material.IsActive && s.Quantity > 0)
                .AsQueryable();

            if (categoryId.HasValue)
                query = query.Where(s => s.Material.CategoryId == categoryId.Value);

            if (locationId.HasValue)
                query = query.Where(s => s.LocationId == locationId.Value);

            var stocks = await query.ToListAsync();

            var items = stocks
                .GroupBy(s => s.MaterialId)
                .Select(g => new MaterialsValueItemViewModel
                {
                    MaterialId = g.Key,
                    MaterialCode = g.First().Material.Code,
                    MaterialName = g.First().Material.Name,
                    CategoryName = g.First().Material.Category?.Name ?? "-",
                    Unit = g.First().Material.Unit,
                    Quantity = g.Sum(s => s.Quantity),
                    UnitPrice = g.First().Material.AveragePrice,
                    TotalValue = g.Sum(s => s.CurrentValue)
                });

            items = sortBy switch
            {
                "quantity" => items.OrderByDescending(i => i.Quantity),
                "name" => items.OrderBy(i => i.MaterialName),
                _ => items.OrderByDescending(i => i.TotalValue)
            };

            var viewModel = new MaterialsValueReportViewModel
            {
                InstitutionName = "المؤسسة",
                Items = items.ToList(),
                SortBy = sortBy,
                CategoryId = categoryId,
                LocationId = locationId
            };

            ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
            ViewBag.Locations = await _context.Locations.Where(l => l.IsActive).OrderBy(l => l.Name).ToListAsync();

            return View(viewModel);
        }

        #endregion

        #region Location Reports

        // GET: Reports/LocationReport
        public async Task<IActionResult> LocationReport(int? id)
        {
            if (id == null)
            {
                var locations = await _context.Locations
                    .Include(l => l.Stocks)
                        .ThenInclude(s => s.Material)
                            .ThenInclude(m => m.Purchases)
                    .Where(l => l.IsActive)
                    .ToListAsync();

                var viewModel = new LocationReportViewModel
                {
                    InstitutionName = "المؤسسة",
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
                        IsActive = l.IsActive,
                        TopMaterials = l.Stocks
                            .Where(s => s.Quantity > 0)
                            .OrderByDescending(s => s.CurrentValue)
                            .Take(5)
                            .Select(s => new TopMaterialViewModel
                            {
                                MaterialName = s.Material.Name,
                                Quantity = s.Quantity,
                                Value = s.CurrentValue
                            }).ToList()
                    }).ToList()
                };

                return View(viewModel);
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

        // GET: Reports/SelectLocation
        public async Task<IActionResult> SelectLocation()
        {
            ViewBag.Locations = await _context.Locations.Where(l => l.IsActive).OrderBy(l => l.Name).ToListAsync();
            return View();
        }

        #endregion

        #region Stock Reports

        // GET: Reports/LowStockReport
        public async Task<IActionResult> LowStockReport(int? categoryId, int? locationId, string? status)
        {
            var stocks = await _context.MaterialStocks
                .Include(s => s.Material)
                    .ThenInclude(m => m.Category)
                .Include(s => s.Material)
                    .ThenInclude(m => m.Purchases)
                .Include(s => s.Location)
                .Where(s => s.Material.IsActive)
                .ToListAsync();

            var lowStockItems = stocks
                .Where(s => s.Quantity <= s.Material.MinimumStock)
                .AsEnumerable();

            if (categoryId.HasValue)
                lowStockItems = lowStockItems.Where(s => s.Material.CategoryId == categoryId.Value);

            if (locationId.HasValue)
                lowStockItems = lowStockItems.Where(s => s.LocationId == locationId.Value);

            if (!string.IsNullOrEmpty(status))
            {
                lowStockItems = status switch
                {
                    "outofstock" => lowStockItems.Where(s => s.Quantity == 0),
                    "critical" => lowStockItems.Where(s => s.Quantity > 0 && (double)s.Quantity / Math.Max(1, s.Material.MinimumStock) < 0.25),
                    "low" => lowStockItems.Where(s => s.Quantity > 0),
                    _ => lowStockItems
                };
            }

            var viewModel = new LowStockReportViewModel
            {
                InstitutionName = "المؤسسة",
                CategoryId = categoryId,
                LocationId = locationId,
                Status = status,
                Items = lowStockItems.Select(s => new LowStockReportItemViewModel
                {
                    MaterialId = s.MaterialId,
                    StockId = s.Id,
                    MaterialCode = s.Material.Code,
                    MaterialName = s.Material.Name,
                    CategoryName = s.Material.Category?.Name ?? "-",
                    LocationName = s.Location.Name,
                    Unit = s.Material.Unit,
                    CurrentQuantity = s.Quantity,
                    MinimumStock = s.Material.MinimumStock
                })
                .OrderBy(i => (double)i.CurrentQuantity / Math.Max(1, i.MinimumStock))
                .ToList()
            };

            ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
            ViewBag.Locations = await _context.Locations.Where(l => l.IsActive).OrderBy(l => l.Name).ToListAsync();

            return View(viewModel);
        }

        // GET: Reports/ExpiryReport
        public async Task<IActionResult> ExpiryReport(string? period, int? categoryId = null, int? locationId = null)
        {
            var query = _context.MaterialStocks
                .Include(s => s.Material)
                    .ThenInclude(m => m.Category)
                .Include(s => s.Location)
                .Where(s => s.ExpiryDate.HasValue && s.Quantity > 0)
                .AsQueryable();

            var today = DateTime.Today;
            if (!string.IsNullOrEmpty(period))
            {
                query = period switch
                {
                    "expired" => query.Where(s => s.ExpiryDate < today),
                    "month" => query.Where(s => s.ExpiryDate >= today && s.ExpiryDate <= today.AddDays(30)),
                    "3months" => query.Where(s => s.ExpiryDate >= today && s.ExpiryDate <= today.AddMonths(3)),
                    "6months" => query.Where(s => s.ExpiryDate >= today && s.ExpiryDate <= today.AddMonths(6)),
                    "year" => query.Where(s => s.ExpiryDate >= today && s.ExpiryDate <= today.AddYears(1)),
                    _ => query.Where(s => s.ExpiryDate <= today.AddYears(1))
                };
            }
            else
            {
                query = query.Where(s => s.ExpiryDate <= today.AddYears(1));
            }

            if (categoryId.HasValue)
                query = query.Where(s => s.Material.CategoryId == categoryId.Value);

            if (locationId.HasValue)
                query = query.Where(s => s.LocationId == locationId.Value);

            var stocks = await query.OrderBy(s => s.ExpiryDate).ToListAsync();

            var viewModel = new ExpiryReportViewModel
            {
                InstitutionName = "المؤسسة",
                Period = period,
                CategoryId = categoryId,
                LocationId = locationId,
                Items = stocks.Select(s => new ExpiryReportItemViewModel
                {
                    StockId = s.Id,
                    MaterialCode = s.Material.Code,
                    MaterialName = s.Material.Name,
                    CategoryName = s.Material.Category?.Name ?? "-",
                    LocationName = s.Location.Name,
                    Quantity = s.Quantity,
                    Unit = s.Material.Unit,
                    ExpiryDate = s.ExpiryDate,
                    UnitPrice = s.Material.AveragePrice
                }).ToList()
            };

            ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
            ViewBag.Locations = await _context.Locations.Where(l => l.IsActive).OrderBy(l => l.Name).ToListAsync();

            return View(viewModel);
        }

        #endregion

        #region Pricing Report

        // GET: Reports/PricingReport
        public async Task<IActionResult> PricingReport(int? categoryId, decimal? minPrice, decimal? maxPrice, string sortBy = "name")
        {
            var query = _context.Materials
                .Include(m => m.Category)
                .Include(m => m.Purchases.OrderByDescending(p => p.PurchaseDate).Take(2))
                .Where(m => m.IsActive)
                .AsQueryable();

            if (categoryId.HasValue)
                query = query.Where(m => m.CategoryId == categoryId.Value);

            var materials = await query.ToListAsync();

            var items = materials.Select(m =>
            {
                var purchases = m.Purchases.OrderByDescending(p => p.PurchaseDate).Take(2).ToList();
                var currentPrice = purchases.FirstOrDefault()?.UnitPrice ?? m.AveragePrice;
                var previousPrice = purchases.Skip(1).FirstOrDefault()?.UnitPrice ?? currentPrice;

                return new PricingReportItemViewModel
                {
                    MaterialId = m.Id,
                    MaterialCode = m.Code,
                    MaterialName = m.Name,
                    CategoryName = m.Category?.Name ?? "-",
                    Unit = m.Unit,
                    PurchasePrice = m.AveragePrice,
                    IssuePrice = m.AveragePrice,
                    LastPurchasePrice = currentPrice,
                    LastPurchaseDate = purchases.FirstOrDefault()?.PurchaseDate
                };
            }).AsEnumerable();

            if (minPrice.HasValue)
                items = items.Where(i => i.PurchasePrice >= minPrice.Value);

            if (maxPrice.HasValue)
                items = items.Where(i => i.PurchasePrice <= maxPrice.Value);

            items = sortBy switch
            {
                "price_asc" => items.OrderBy(i => i.PurchasePrice),
                "price_desc" => items.OrderByDescending(i => i.PurchasePrice),
                "code" => items.OrderBy(i => i.MaterialCode),
                _ => items.OrderBy(i => i.MaterialName)
            };

            var viewModel = new PricingReportViewModel
            {
                InstitutionName = "المؤسسة",
                CategoryId = categoryId,
                MinPriceFilter = minPrice,
                MaxPriceFilter = maxPrice,
                SortBy = sortBy,
                Items = items.ToList()
            };

            ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();

            return View(viewModel);
        }

        // GET: Reports/SelectMaterial
        public async Task<IActionResult> SelectMaterial()
        {
            ViewBag.Materials = await _context.Materials.Where(m => m.IsActive).OrderBy(m => m.Name).ToListAsync();
            return View();
        }

        #endregion

        #region Purchase Reports

        // GET: Reports/PurchasesYearly - إصلاح خطأ LINQ
        public async Task<IActionResult> PurchasesYearly(int? year)
        {
            year ??= DateTime.Now.Year;

            // جلب البيانات أولاً ثم حساب الإجماليات في الذاكرة
            var purchases = await _context.Purchases
                .Include(p => p.Material)
                    .ThenInclude(m => m.Category)
                .Include(p => p.Location)
                .Where(p => p.PurchaseDate.Year == year)
                .OrderBy(p => p.PurchaseDate)
                .ToListAsync();

            // جلب بيانات السنة السابقة أيضاً في الذاكرة
            var previousYearPurchases = await _context.Purchases
                .Where(p => p.PurchaseDate.Year == year - 1)
                .ToListAsync();

            // حساب الإجمالي في الذاكرة بدلاً من SQL
            var previousYearTotal = previousYearPurchases.Sum(p => p.TotalPrice);

            var viewModel = new PurchasesYearlyReportViewModel
            {
                Year = year.Value,
                InstitutionName = "المؤسسة",
                TotalPurchases = purchases.Sum(p => p.TotalPrice),
                TotalOrders = purchases.Count,
                TotalSuppliers = purchases.Where(p => !string.IsNullOrEmpty(p.Supplier)).Select(p => p.Supplier).Distinct().Count(),
                PreviousYearTotal = previousYearTotal,

                MonthlyData = Enumerable.Range(1, 12).Select(month => new MonthlyPurchaseDataViewModel
                {
                    Month = month,
                    OrderCount = purchases.Count(p => p.PurchaseDate.Month == month),
                    TotalAmount = purchases.Where(p => p.PurchaseDate.Month == month).Sum(p => p.TotalPrice)
                }).ToList(),

                CategorySummary = purchases
                    .Where(p => p.Material.Category != null)
                    .GroupBy(p => p.Material.Category!.Name)
                    .Select(g => new CategoryPurchaseSummaryViewModel
                    {
                        CategoryName = g.Key,
                        OrderCount = g.Count(),
                        TotalAmount = g.Sum(p => p.TotalPrice)
                    }).ToList(),

                SupplierSummary = purchases
                    .Where(p => !string.IsNullOrEmpty(p.Supplier))
                    .GroupBy(p => p.Supplier!)
                    .Select(g => new SupplierSummaryViewModel
                    {
                        SupplierName = g.Key,
                        OrderCount = g.Count(),
                        TotalAmount = g.Sum(p => p.TotalPrice)
                    })
                    .OrderByDescending(s => s.TotalAmount)
                    .Take(10)
                    .ToList()
            };

            ViewBag.AvailableYears = await _context.Purchases
                .Select(p => p.PurchaseDate.Year)
                .Distinct()
                .OrderByDescending(y => y)
                .ToListAsync();

            return View(viewModel);
        }

        #endregion

        #region Government Forms

        // GET: Reports/InventoryForm2
        public async Task<IActionResult> InventoryForm2(int? year, int? categoryId, int? locationId)
        {
            year ??= DateTime.Now.Year;

            var query = _context.MaterialStocks
                .Include(s => s.Material)
                    .ThenInclude(m => m.Category)
                .Include(s => s.Location)
                .Where(s => s.Material.IsActive)
                .AsQueryable();

            if (categoryId.HasValue)
                query = query.Where(s => s.Material.CategoryId == categoryId.Value);

            if (locationId.HasValue)
                query = query.Where(s => s.LocationId == locationId.Value);

            var stocks = await query.OrderBy(s => s.Material.Category!.Name)
                                    .ThenBy(s => s.Material.Name)
                                    .ToListAsync();

            // استخدام النوع الصحيح من namespace المشروع
            var viewModel = new WarehouseManagement.Models.ViewModels.InventoryForm2ViewModel
            {
                Year = year.Value,
                InventoryDate = DateTime.Now,
                FormNumber = $"INV-{year}-{DateTime.Now:MMdd}",
                InstitutionName = "المؤسسة",
                WarehouseName = "المخزن الرئيسي",
                WarehouseKeeper = "أمين المخزن",
                CommitteeHead = "رئيس لجنة الجرد",
                CategoryId = categoryId,
                LocationId = locationId,

                Items = stocks.Select(s => new WarehouseManagement.Models.ViewModels.InventoryForm2ItemViewModel
                {
                    MaterialCode = s.Material.Code,
                    MaterialName = s.Material.Name,
                    Unit = s.Material.Unit,
                    BookQuantity = s.Quantity,
                    BookValue = s.CurrentValue,
                    ActualQuantity = s.Quantity,
                    ActualValue = s.CurrentValue,
                    Notes = ""
                }).ToList()
            };

            ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();
            ViewBag.Locations = await _context.Locations.Where(l => l.IsActive).OrderBy(l => l.Name).ToListAsync();

            return View(viewModel);
        }

        // GET: Reports/ConsumptionForm5
        public async Task<IActionResult> ConsumptionForm5(int? year, int? month, int? departmentId, int? categoryId)
        {
            year ??= DateTime.Now.Year;
            month ??= DateTime.Now.Month;

            var startDate = new DateTime(year.Value, month.Value, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var viewModel = new WarehouseManagement.Models.ViewModels.ConsumptionForm5ViewModel
            {
                Year = year.Value,
                Month = month.Value,
                FormNumber = $"CON-{year}-{month:D2}",
                DepartmentId = departmentId,
                CategoryId = categoryId,
                InstitutionName = "المؤسسة",
                WarehouseName = "المخزن الرئيسي",
                WarehouseKeeper = "أمين المخزن",
                Items = new List<WarehouseManagement.Models.ViewModels.ConsumptionForm5ItemViewModel>()
            };

            // جلب سجلات الاستهلاك
            var consumptions = await _context.ConsumptionRecords
                .Include(c => c.InventoryRecord)
                    .ThenInclude(i => i.Material)
                        .ThenInclude(m => m.Category)
                .Where(c => c.ReportDate >= startDate && c.ReportDate <= endDate)
                .ToListAsync();

            if (categoryId.HasValue)
                consumptions = consumptions.Where(c => c.InventoryRecord.Material.CategoryId == categoryId.Value).ToList();

            viewModel.Items = consumptions.Select(c => new WarehouseManagement.Models.ViewModels.ConsumptionForm5ItemViewModel
            {
                IssueDate = c.ReportDate,
                IssueNumber = c.Id.ToString(),
                MaterialCode = c.InventoryRecord.Material.Code,
                MaterialName = c.InventoryRecord.Material.Name,
                CategoryName = c.InventoryRecord.Material.Category?.Name ?? "-",
                Unit = c.InventoryRecord.Material.Unit,
                Quantity = c.ConsumedQuantity,
                UnitPrice = c.OriginalUnitPrice,
                DepartmentName = c.InventoryRecord.Department ?? "-",
                ReceivedBy = c.CreatedBy ?? "-",
                Notes = c.Notes
            }).ToList();

            ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();

            return View(viewModel);
        }

        #endregion

        #region Export Actions

        public IActionResult ExportForm2(int year)
        {
            TempData["Info"] = "سيتم إضافة خاصية التصدير قريباً";
            return RedirectToAction(nameof(InventoryForm2), new { year });
        }

        public IActionResult ExportForm5(int year, int month)
        {
            TempData["Info"] = "سيتم إضافة خاصية التصدير قريباً";
            return RedirectToAction(nameof(ConsumptionForm5), new { year, month });
        }

        public IActionResult ExportMaterialsValue()
        {
            TempData["Info"] = "سيتم إضافة خاصية التصدير قريباً";
            return RedirectToAction(nameof(MaterialsValue));
        }

        public IActionResult ExportLowStock()
        {
            TempData["Info"] = "سيتم إضافة خاصية التصدير قريباً";
            return RedirectToAction(nameof(LowStockReport));
        }

        public IActionResult ExportExpiry()
        {
            TempData["Info"] = "سيتم إضافة خاصية التصدير قريباً";
            return RedirectToAction(nameof(ExpiryReport));
        }

        public IActionResult ExportPricing()
        {
            TempData["Info"] = "سيتم إضافة خاصية التصدير قريباً";
            return RedirectToAction(nameof(PricingReport));
        }

        public IActionResult ExportPurchasesYearly(int year)
        {
            TempData["Info"] = "سيتم إضافة خاصية التصدير قريباً";
            return RedirectToAction(nameof(PurchasesYearly), new { year });
        }

        public IActionResult ExportLocationReport()
        {
            TempData["Info"] = "سيتم إضافة خاصية التصدير قريباً";
            return RedirectToAction(nameof(LocationReport));
        }

        #endregion
    }
}
