using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Data;
using WarehouseManagement.Models;
using WarehouseManagement.Models.ViewModels;

namespace WarehouseManagement.Services
{
    /// <summary>
    /// واجهة خدمة التقارير
    /// </summary>
    public interface IReportService
    {
        Task<DashboardViewModel> GetDashboardDataAsync();
        Task<LocationReportViewModel> GetLocationReportAsync(int locationId);
        Task<InventoryStatistics> GetInventoryStatisticsAsync(int year);
        Task<ConsumptionStatistics> GetConsumptionStatisticsAsync(int year, string? department = null);
        Task<List<MaterialConsumptionSummary>> GetTopConsumedMaterialsAsync(DateTime fromDate, DateTime toDate, int count = 10);
        Task<PricingReportViewModel> GetMaterialPricingReportAsync(int materialId);
    }

    /// <summary>
    /// تنفيذ خدمة التقارير
    /// </summary>
    public class ReportService : IReportService
    {
        private readonly WarehouseContext _context;

        public ReportService(WarehouseContext context)
        {
            _context = context;
        }

        public async Task<DashboardViewModel> GetDashboardDataAsync()
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

            // حساب المخزون المنخفض
            var stocks = await _context.MaterialStocks
                .Include(s => s.Material)
                .Where(s => s.Quantity > 0 && s.Material.IsActive)
                .ToListAsync();

            viewModel.LowStockItems = stocks.Count(s => s.Quantity <= s.Material.MinimumStock);

            // المواد التي ستنتهي صلاحيتها
            viewModel.ExpiringItems = await _context.MaterialStocks
                .CountAsync(s => s.ExpiryDate.HasValue &&
                               s.ExpiryDate <= DateTime.Now.AddDays(30) &&
                               s.ExpiryDate > DateTime.Now);

            // القيمة الإجمالية
            var materials = await _context.Materials
                .Include(m => m.Stocks)
                .Include(m => m.Purchases)
                .Where(m => m.IsActive)
                .ToListAsync();

            viewModel.TotalValue = materials.Sum(m => m.TotalValue);

            // آخر العمليات
            viewModel.RecentTransfers = await _context.Transfers
                .Include(t => t.Material)
                .Include(t => t.FromLocation)
                .Include(t => t.ToLocation)
                .OrderByDescending(t => t.TransferDate)
                .Take(5)
                .ToListAsync();

            viewModel.RecentPurchases = await _context.Purchases
                .Include(p => p.Material)
                .Include(p => p.Location)
                .OrderByDescending(p => p.PurchaseDate)
                .Take(5)
                .ToListAsync();

            viewModel.CriticalItems = await _context.MaterialStocks
                .Include(s => s.Material)
                    .ThenInclude(m => m.Category)
                .Include(s => s.Location)
                .Where(s => s.Material.IsActive)
                .ToListAsync();

            viewModel.CriticalItems = viewModel.CriticalItems
                .Where(s => s.Quantity <= s.Material.MinimumStock ||
                           (s.ExpiryDate.HasValue && s.ExpiryDate <= DateTime.Now.AddDays(7)))
                .Take(10)
                .ToList();

            return viewModel;
        }

        public async Task<LocationReportViewModel> GetLocationReportAsync(int locationId)
        {
            var location = await _context.Locations.FindAsync(locationId);
            if (location == null)
                throw new ArgumentException("الموقع غير موجود");

            var stocks = await _context.MaterialStocks
                .Include(s => s.Material)
                    .ThenInclude(m => m.Category)
                .Include(s => s.Material)
                    .ThenInclude(m => m.Purchases)
                .Where(s => s.LocationId == locationId && s.Quantity > 0)
                .OrderBy(s => s.Material.Category!.Name)
                .ThenBy(s => s.Material.Name)
                .ToListAsync();

            return new LocationReportViewModel
            {
                Location = location,
                Stocks = stocks,
                TotalItems = stocks.Count,
                TotalQuantity = stocks.Sum(s => s.Quantity),
                TotalValue = stocks.Sum(s => s.CurrentValue)
            };
        }

        public async Task<InventoryStatistics> GetInventoryStatisticsAsync(int year)
        {
            var records = await _context.InventoryRecords
                .Include(i => i.Material)
                    .ThenInclude(m => m.Purchases)
                .Include(i => i.Consumptions)
                .Where(i => i.Year == year)
                .ToListAsync();

            return new InventoryStatistics
            {
                TotalItems = records.Count,
                TotalQuantityByInventory = records.Sum(r => r.ActualQuantity),
                TotalQuantityByRecords = records.Sum(r => r.RecordedQuantity),
                TotalDifference = records.Sum(r => r.Difference),
                TotalCost = records.Sum(r => r.TotalCost),
                ItemsWithShortage = records.Count(r => r.HasShortage),
                ItemsWithSurplus = records.Count(r => r.HasSurplus),
                ItemsMatching = records.Count(r => r.IsMatching),
                ItemsWithConsumption = records.Count(r => r.HasConsumptions)
            };
        }

        public async Task<ConsumptionStatistics> GetConsumptionStatisticsAsync(int year, string? department = null)
        {
            var query = _context.ConsumptionRecords
                .Include(c => c.InventoryRecord)
                .Where(c => c.ReportDate.Year == year);

            if (!string.IsNullOrEmpty(department))
                query = query.Where(c => c.InventoryRecord.Department == department);

            var records = await query.ToListAsync();

            return new ConsumptionStatistics
            {
                TotalItems = records.Count,
                TotalQuantity = records.Sum(c => c.ConsumedQuantity),
                TotalOriginalValue = records.Sum(c => c.StoredOriginalValue),
                TotalResidualValue = records.Sum(c => c.StoredResidualValue),
                ItemsDisposed = records.Count(c => c.IsDisposed),
                ItemsPending = records.Count(c => !c.IsDisposed),
                ByReason = records.GroupBy(c => c.Reason).ToDictionary(g => g.Key, g => g.Count()),
                ByDecision = records.GroupBy(c => c.Decision).ToDictionary(g => g.Key, g => g.Count())
            };
        }

        public async Task<List<MaterialConsumptionSummary>> GetTopConsumedMaterialsAsync(DateTime fromDate, DateTime toDate, int count = 10)
        {
            var consumptions = await _context.ConsumptionRecords
                .Include(c => c.InventoryRecord)
                    .ThenInclude(i => i.Material)
                        .ThenInclude(m => m.Category)
                .Where(c => c.ReportDate >= fromDate && c.ReportDate <= toDate)
                .ToListAsync();

            return consumptions
                .GroupBy(c => new { c.InventoryRecord.MaterialId, c.InventoryRecord.Material.Name, c.InventoryRecord.Material.Code })
                .Select(g => new MaterialConsumptionSummary
                {
                    MaterialId = g.Key.MaterialId,
                    MaterialName = g.Key.Name,
                    MaterialCode = g.Key.Code,
                    CategoryName = g.First().InventoryRecord.Material.Category?.Name,
                    TotalConsumedQuantity = g.Sum(c => c.ConsumedQuantity),
                    TotalLossValue = g.Sum(c => c.StoredOriginalValue - c.StoredResidualValue),
                    AverageDamagePercentage = g.Average(c => (double)(c.DamagePercentage ?? 0)),
                    ConsumptionCount = g.Count(),
                    MostCommonReason = g.GroupBy(c => c.Reason).OrderByDescending(rg => rg.Count()).First().Key
                })
                .OrderByDescending(s => s.TotalLossValue)
                .Take(count)
                .ToList();
        }

        public async Task<PricingReportViewModel> GetMaterialPricingReportAsync(int materialId)
        {
            var material = await _context.Materials
                .Include(m => m.Category)
                .Include(m => m.Stocks)
                    .ThenInclude(s => s.Location)
                .Include(m => m.Purchases.OrderBy(p => p.PurchaseDate))
                    .ThenInclude(p => p.Location)
                .FirstOrDefaultAsync(m => m.Id == materialId);

            if (material == null)
                throw new ArgumentException("المادة غير موجودة");

            return new PricingReportViewModel
            {
                Material = material,
                Purchases = material.Purchases.ToList(),
                TotalQuantityPurchased = material.Purchases.Sum(p => p.Quantity),
                TotalAmountSpent = material.Purchases.Sum(p => p.TotalPrice),
                CurrentValue = material.TotalValue,
                FirstPurchaseDate = material.Purchases.FirstOrDefault()?.PurchaseDate,
                LastPurchaseDate = material.Purchases.LastOrDefault()?.PurchaseDate
            };
        }
    }
}
