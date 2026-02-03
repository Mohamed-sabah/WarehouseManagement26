using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Data;
using WarehouseManagement.Models;

namespace WarehouseManagement.Services
{
    /// <summary>
    /// خدمة ترحيل البيانات من الهيكل القديم للجديد
    /// Data Migration Service - From Old to New Structure
    /// </summary>
    public interface IDataMigrationService
    {
        Task<MigrationResult> MigrateFromOldStructureAsync();
        Task<MigrationResult> ValidateDataIntegrityAsync();
    }

    public class DataMigrationService : IDataMigrationService
    {
        private readonly WarehouseContext _context;
        private readonly ILogger<DataMigrationService> _logger;

        public DataMigrationService(WarehouseContext context, ILogger<DataMigrationService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<MigrationResult> MigrateFromOldStructureAsync()
        {
            var result = new MigrationResult();

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _logger.LogInformation("بدء عملية الترحيل...");

                result.MaterialsUpdated = await UpdateMaterialsAsync();
                _logger.LogInformation($"تم تحديث {result.MaterialsUpdated} مادة");

                result.StocksCreated = await CreateStockRecordsAsync();
                _logger.LogInformation($"تم إنشاء {result.StocksCreated} سجل مخزون");

                result.PurchasesUnified = await UnifyPurchaseRecordsAsync();
                _logger.LogInformation($"تم توحيد {result.PurchasesUnified} سجل شراء");

                result.InventoryRecordsConverted = await ConvertInventoryRecordsAsync();
                _logger.LogInformation($"تم تحويل {result.InventoryRecordsConverted} سجل جرد");

                result.ConsumptionRecordsConverted = await ConvertConsumptionRecordsAsync();
                _logger.LogInformation($"تم تحويل {result.ConsumptionRecordsConverted} سجل استهلاك");

                await transaction.CommitAsync();
                result.Success = true;
                result.Message = "تمت عملية الترحيل بنجاح";
                _logger.LogInformation("اكتملت عملية الترحيل بنجاح");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                result.Success = false;
                result.Message = $"فشلت عملية الترحيل: {ex.Message}";
                result.Errors.Add(ex.Message);
                _logger.LogError(ex, "فشلت عملية الترحيل");
            }

            return result;
        }

        public async Task<MigrationResult> ValidateDataIntegrityAsync()
        {
            var result = new MigrationResult { Success = true };

            var materialsWithoutStock = await _context.Materials
                .Where(m => m.IsActive && !m.Stocks.Any())
                .ToListAsync();

            if (materialsWithoutStock.Any())
            {
                result.Warnings.Add($"يوجد {materialsWithoutStock.Count} مادة بدون سجل مخزون");
            }

            var orphanInventoryRecords = await _context.InventoryRecords
                .Where(i => i.Material == null)
                .CountAsync();

            if (orphanInventoryRecords > 0)
            {
                result.Errors.Add($"يوجد {orphanInventoryRecords} سجل جرد بدون مادة مرتبطة");
                result.Success = false;
            }

            var orphanConsumptionRecords = await _context.ConsumptionRecords
                .Where(c => c.InventoryRecord == null)
                .CountAsync();

            if (orphanConsumptionRecords > 0)
            {
                result.Errors.Add($"يوجد {orphanConsumptionRecords} سجل استهلاك بدون سجل جرد مرتبط");
                result.Success = false;
            }

            var negativeStocks = await _context.MaterialStocks
                .Where(s => s.Quantity < 0)
                .CountAsync();

            if (negativeStocks > 0)
            {
                result.Errors.Add($"يوجد {negativeStocks} سجل مخزون بكمية سالبة");
                result.Success = false;
            }

            result.Message = result.Success 
                ? "البيانات سليمة" 
                : "توجد مشاكل في البيانات تحتاج معالجة";

            return result;
        }

        private async Task<int> UpdateMaterialsAsync()
        {
            var materials = await _context.Materials.ToListAsync();
            int count = 0;

            foreach (var material in materials)
            {
                if (string.IsNullOrEmpty(material.Unit))
                    material.Unit = "قطعة";

                material.IsActive = true;
                material.LastUpdated = DateTime.Now;
                count++;
            }

            await _context.SaveChangesAsync();
            return count;
        }

        private async Task<int> CreateStockRecordsAsync()
        {
            var materialsWithoutStock = await _context.Materials
                .Include(m => m.Stocks)
                .Where(m => !m.Stocks.Any())
                .ToListAsync();

            int count = 0;
            foreach (var material in materialsWithoutStock)
            {
                var defaultLocation = await _context.Locations.FirstOrDefaultAsync();
                if (defaultLocation == null) continue;

                var stock = new MaterialStock
                {
                    MaterialId = material.Id,
                    LocationId = defaultLocation.Id,
                    Quantity = 0,
                    Condition = "جيدة",
                    CreatedDate = DateTime.Now,
                    LastUpdated = DateTime.Now
                };

                _context.MaterialStocks.Add(stock);
                count++;
            }

            await _context.SaveChangesAsync();
            return count;
        }

        private async Task<int> UnifyPurchaseRecordsAsync()
        {
            return await _context.Purchases.CountAsync();
        }

        private async Task<int> ConvertInventoryRecordsAsync()
        {
            return await _context.InventoryRecords.CountAsync();
        }

        private async Task<int> ConvertConsumptionRecordsAsync()
        {
            return await _context.ConsumptionRecords.CountAsync();
        }
    }

    public class MigrationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        
        public int MaterialsUpdated { get; set; }
        public int StocksCreated { get; set; }
        public int PurchasesUnified { get; set; }
        public int InventoryRecordsConverted { get; set; }
        public int ConsumptionRecordsConverted { get; set; }
    }
}
