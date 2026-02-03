using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Data;
using WarehouseManagement.Models.ViewModels;

namespace WarehouseManagement.Controllers
{
    public class DashboardController : Controller
    {
        private readonly WarehouseContext _context;

        public DashboardController(WarehouseContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = new DashboardViewModel
            {
                TotalMaterials = await _context.Materials.CountAsync(m => m.IsActive),
                TotalLocations = await _context.Locations.CountAsync(l => l.IsActive),
                TotalCategories = await _context.Categories.CountAsync(c => c.IsActive),
                PendingTransfers = await _context.Transfers.CountAsync(t => !t.IsConfirmed),
                TotalInventoryRecords = await _context.InventoryRecords.CountAsync(i => i.Year == DateTime.Now.Year),
                TotalConsumptionRecords = await _context.ConsumptionRecords.CountAsync()
            };

            var stocks = await _context.MaterialStocks
                .Include(s => s.Material)
                .Where(s => s.Quantity > 0)
                .ToListAsync();

            viewModel.LowStockItems = stocks.Count(s => s.Material.IsLowStock);

            viewModel.ExpiringItems = await _context.MaterialStocks
                .CountAsync(s => s.ExpiryDate.HasValue && 
                               s.ExpiryDate <= DateTime.Now.AddDays(30) &&
                               s.ExpiryDate > DateTime.Now);

            var materials = await _context.Materials
                .Include(m => m.Stocks)
                .Include(m => m.Purchases)
                .Where(m => m.IsActive)
                .ToListAsync();

            viewModel.TotalValue = materials.Sum(m => m.TotalValue);

            viewModel.RecentTransfers = await _context.Transfers
                .Include(t => t.Material)
                .Include(t => t.FromLocation)
                .Include(t => t.ToLocation)
                .OrderByDescending(t => t.TransferDate)
                .Take(5)
                .ToListAsync();

            viewModel.CriticalItems = await _context.MaterialStocks
                .Include(s => s.Material)
                    .ThenInclude(m => m.Category)
                .Include(s => s.Location)
                .Where(s => s.Quantity <= s.Material.MinimumStock ||
                           (s.ExpiryDate.HasValue && s.ExpiryDate <= DateTime.Now.AddDays(7)))
                .Take(10)
                .ToListAsync();

            viewModel.RecentPurchases = await _context.Purchases
                .Include(p => p.Material)
                .Include(p => p.Location)
                .OrderByDescending(p => p.PurchaseDate)
                .Take(5)
                .ToListAsync();

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetChartData(string type)
        {
            switch (type?.ToLower())
            {
                case "materials-by-category":
                    var categoryData = await _context.Categories
                        .Where(c => c.IsActive)
                        .Select(c => new
                        {
                            name = c.Name,
                            count = c.Materials.Count(m => m.IsActive)
                        })
                        .Where(x => x.count > 0)
                        .ToListAsync();
                    return Json(categoryData);

                case "materials-by-location":
                    var locationData = await _context.Locations
                        .Where(l => l.IsActive)
                        .Select(l => new
                        {
                            name = l.Name,
                            count = l.Stocks.Sum(s => s.Quantity)
                        })
                        .Where(x => x.count > 0)
                        .ToListAsync();
                    return Json(locationData);

                case "purchases-by-month":
                    var purchaseData = await _context.Purchases
                        .Where(p => p.PurchaseDate >= DateTime.Now.AddMonths(-6))
                        .GroupBy(p => new { p.PurchaseDate.Year, p.PurchaseDate.Month })
                        .Select(g => new
                        {
                            month = $"{g.Key.Year}-{g.Key.Month:00}",
                            count = g.Count(),
                            value = g.Sum(p => p.StoredTotalPrice)
                        })
                        .OrderBy(x => x.month)
                        .ToListAsync();
                    return Json(purchaseData);

                case "consumption-by-reason":
                    var consumptionData = await _context.ConsumptionRecords
                        .GroupBy(c => c.Reason)
                        .Select(g => new
                        {
                            reason = g.Key.ToString(),
                            count = g.Count(),
                            value = g.Sum(c => c.StoredOriginalValue)
                        })
                        .ToListAsync();
                    return Json(consumptionData);

                default:
                    return BadRequest("نوع البيانات غير مدعوم");
            }
        }
    }
}
