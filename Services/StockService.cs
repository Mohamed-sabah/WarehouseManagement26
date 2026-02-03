using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Data;
using WarehouseManagement.Models;

namespace WarehouseManagement.Services
{
    /// <summary>
    /// واجهة خدمة المخزون
    /// </summary>
    public interface IStockService
    {
        Task<MaterialStock?> GetStockAsync(int materialId, int locationId);
        Task<bool> AddToStockAsync(int materialId, int locationId, int quantity, string? notes = null);
        Task<bool> DeductFromStockAsync(int materialId, int locationId, int quantity, string? notes = null);
        Task<bool> TransferStockAsync(int materialId, int fromLocationId, int toLocationId, int quantity, string? notes = null);
        Task<List<MaterialStock>> GetLowStockItemsAsync();
        Task<List<MaterialStock>> GetExpiringItemsAsync(int days = 30);
        Task<int> GetTotalQuantityAsync(int materialId);
        Task<decimal> GetTotalValueAsync(int materialId);
    }

    /// <summary>
    /// تنفيذ خدمة المخزون
    /// </summary>
    public class StockService : IStockService
    {
        private readonly WarehouseContext _context;

        public StockService(WarehouseContext context)
        {
            _context = context;
        }

        public async Task<MaterialStock?> GetStockAsync(int materialId, int locationId)
        {
            return await _context.MaterialStocks
                .Include(s => s.Material)
                .Include(s => s.Location)
                .FirstOrDefaultAsync(s => s.MaterialId == materialId && s.LocationId == locationId);
        }

        public async Task<bool> AddToStockAsync(int materialId, int locationId, int quantity, string? notes = null)
        {
            var stock = await GetStockAsync(materialId, locationId);

            if (stock != null)
            {
                stock.Quantity += quantity;
                stock.LastUpdated = DateTime.Now;
                if (!string.IsNullOrEmpty(notes))
                    stock.Notes = $"{stock.Notes}\n[{DateTime.Now:yyyy-MM-dd}] إضافة {quantity}: {notes}";
            }
            else
            {
                stock = new MaterialStock
                {
                    MaterialId = materialId,
                    LocationId = locationId,
                    Quantity = quantity,
                    Condition = "جيدة",
                    CreatedDate = DateTime.Now,
                    LastUpdated = DateTime.Now,
                    Notes = notes
                };
                _context.MaterialStocks.Add(stock);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeductFromStockAsync(int materialId, int locationId, int quantity, string? notes = null)
        {
            var stock = await GetStockAsync(materialId, locationId);

            if (stock == null || stock.Quantity < quantity)
                return false;

            stock.Quantity -= quantity;
            stock.LastUpdated = DateTime.Now;
            if (!string.IsNullOrEmpty(notes))
                stock.Notes = $"{stock.Notes}\n[{DateTime.Now:yyyy-MM-dd}] خصم {quantity}: {notes}";

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> TransferStockAsync(int materialId, int fromLocationId, int toLocationId, int quantity, string? notes = null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // خصم من المصدر
                var deducted = await DeductFromStockAsync(materialId, fromLocationId, quantity, $"نقل إلى موقع آخر - {notes}");
                if (!deducted)
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                // إضافة للهدف
                await AddToStockAsync(materialId, toLocationId, quantity, $"منقول من موقع آخر - {notes}");

                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<MaterialStock>> GetLowStockItemsAsync()
        {
            var stocks = await _context.MaterialStocks
                .Include(s => s.Material)
                    .ThenInclude(m => m.Category)
                .Include(s => s.Location)
                .Where(s => s.Material.IsActive)
                .ToListAsync();

            return stocks.Where(s => s.Quantity <= s.Material.MinimumStock).ToList();
        }

        public async Task<List<MaterialStock>> GetExpiringItemsAsync(int days = 30)
        {
            var expiryDate = DateTime.Now.AddDays(days);
            return await _context.MaterialStocks
                .Include(s => s.Material)
                    .ThenInclude(m => m.Category)
                .Include(s => s.Location)
                .Where(s => s.ExpiryDate.HasValue && s.ExpiryDate <= expiryDate && s.Quantity > 0)
                .OrderBy(s => s.ExpiryDate)
                .ToListAsync();
        }

        public async Task<int> GetTotalQuantityAsync(int materialId)
        {
            return await _context.MaterialStocks
                .Where(s => s.MaterialId == materialId)
                .SumAsync(s => s.Quantity);
        }

        public async Task<decimal> GetTotalValueAsync(int materialId)
        {
            var material = await _context.Materials
                .Include(m => m.Stocks)
                .Include(m => m.Purchases)
                .FirstOrDefaultAsync(m => m.Id == materialId);

            return material?.TotalValue ?? 0;
        }
    }
}
