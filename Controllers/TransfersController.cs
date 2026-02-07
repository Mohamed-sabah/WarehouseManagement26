using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Data;
using WarehouseManagement.Models;

namespace WarehouseManagement.Controllers
{
    public class TransfersController : Controller
    {
        private readonly WarehouseContext _context;

        public TransfersController(WarehouseContext context)
        {
            _context = context;
        }

        // GET: Transfers
        public async Task<IActionResult> Index(int? materialId, int? fromLocationId, int? toLocationId, bool pendingOnly = false)
        {
            var query = _context.Transfers
                .Include(t => t.Material)
                .Include(t => t.FromLocation)
                .Include(t => t.ToLocation)
                .AsQueryable();

            if (materialId.HasValue)
                query = query.Where(t => t.MaterialId == materialId.Value);

            if (fromLocationId.HasValue)
                query = query.Where(t => t.FromLocationId == fromLocationId.Value);

            if (toLocationId.HasValue)
                query = query.Where(t => t.ToLocationId == toLocationId.Value);

            if (pendingOnly)
                query = query.Where(t => !t.IsConfirmed);

            var transfers = await query.OrderByDescending(t => t.TransferDate).ToListAsync();

            ViewBag.Materials = await _context.Materials.Where(m => m.IsActive).OrderBy(m => m.Name).ToListAsync();
            ViewBag.Locations = await _context.Locations.Where(l => l.IsActive).OrderBy(l => l.Name).ToListAsync();
            ViewBag.PendingCount = await _context.Transfers.CountAsync(t => !t.IsConfirmed);

            return View(transfers);
        }

        // GET: Transfers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var transfer = await _context.Transfers
                .Include(t => t.Material)
                    .ThenInclude(m => m.Category)
                .Include(t => t.FromLocation)
                .Include(t => t.ToLocation)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transfer == null) return NotFound();

            return View(transfer);
        }

        // GET: Transfers/Create
        public async Task<IActionResult> Create(int? materialId, int? fromLocationId)
        {
            var transfer = new Transfer
            {
                TransferDate = DateTime.Now
            };

            if (materialId.HasValue)
                transfer.MaterialId = materialId.Value;

            if (fromLocationId.HasValue)
                transfer.FromLocationId = fromLocationId.Value;

            await LoadSelectLists(materialId, fromLocationId);
            return View(transfer);
        }

        // POST: Transfers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaterialId,FromLocationId,ToLocationId,Quantity,TransferDate,Notes,TransferredBy")] Transfer transfer)
        {
            if (transfer.FromLocationId == transfer.ToLocationId)
            {
                ModelState.AddModelError("ToLocationId", "لا يمكن النقل لنفس الموقع");
            }

            // التحقق من توفر الكمية
            var sourceStock = await _context.MaterialStocks
                .FirstOrDefaultAsync(s => s.MaterialId == transfer.MaterialId && 
                                         s.LocationId == transfer.FromLocationId);

            if (sourceStock == null || sourceStock.Quantity < transfer.Quantity)
            {
                ModelState.AddModelError("Quantity", 
                    $"الكمية المطلوبة غير متوفرة. المتوفر: {sourceStock?.Quantity ?? 0}");
            }

            //if (ModelState.IsValid)
            //{
                transfer.CreatedDate = DateTime.Now;
                _context.Transfers.Add(transfer);
                await _context.SaveChangesAsync();

                TempData["Success"] = "تم إنشاء طلب النقل بنجاح. في انتظار التأكيد.";
                return RedirectToAction(nameof(Details), new { id = transfer.Id });
            //}

            await LoadSelectLists(transfer.MaterialId, transfer.FromLocationId);
            return View(transfer);
        }

        // POST: Transfers/Confirm/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(int id, string? confirmedBy)
        {
            var transfer = await _context.Transfers.FindAsync(id);
            if (transfer == null) return NotFound();

            if (transfer.IsConfirmed)
            {
                TempData["Warning"] = "تم تأكيد هذا النقل مسبقاً";
                return RedirectToAction(nameof(Details), new { id });
            }

            // التحقق من توفر الكمية مرة أخرى
            var sourceStock = await _context.MaterialStocks
                .FirstOrDefaultAsync(s => s.MaterialId == transfer.MaterialId && 
                                         s.LocationId == transfer.FromLocationId);

            if (sourceStock == null || sourceStock.Quantity < transfer.Quantity)
            {
                TempData["Error"] = "الكمية المطلوبة لم تعد متوفرة في المصدر";
                return RedirectToAction(nameof(Details), new { id });
            }

            using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // خصم من المصدر
                sourceStock.Quantity -= transfer.Quantity;
                sourceStock.LastUpdated = DateTime.Now;

                // إضافة للهدف
                var targetStock = await _context.MaterialStocks
                    .FirstOrDefaultAsync(s => s.MaterialId == transfer.MaterialId && 
                                             s.LocationId == transfer.ToLocationId);

                if (targetStock != null)
                {
                    targetStock.Quantity += transfer.Quantity;
                    targetStock.LastUpdated = DateTime.Now;
                }
                else
                {
                    targetStock = new MaterialStock
                    {
                        MaterialId = transfer.MaterialId,
                        LocationId = transfer.ToLocationId,
                        Quantity = transfer.Quantity,
                        Condition = sourceStock.Condition,
                        CreatedDate = DateTime.Now,
                        LastUpdated = DateTime.Now
                    };
                    _context.MaterialStocks.Add(targetStock);
                }

                transfer.IsConfirmed = true;
                transfer.ConfirmedDate = DateTime.Now;
                transfer.ConfirmedBy = confirmedBy;
                transfer.IsExecuted = true;
                transfer.ExecutedDate = DateTime.Now;

                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                TempData["Success"] = "تم تأكيد وتنفيذ النقل بنجاح";
            }
            catch
            {
                await dbTransaction.RollbackAsync();
                TempData["Error"] = "حدث خطأ أثناء تنفيذ النقل";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Transfers/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, string? reason)
        {
            var transfer = await _context.Transfers.FindAsync(id);
            if (transfer == null) return NotFound();

            if (transfer.IsExecuted)
            {
                TempData["Error"] = "لا يمكن إلغاء نقل تم تنفيذه";
                return RedirectToAction(nameof(Details), new { id });
            }

            transfer.IsCancelled = true;
            transfer.Notes = $"{transfer.Notes}\n[إلغاء] {reason}";
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم إلغاء طلب النقل";
            return RedirectToAction(nameof(Index));
        }

        // GET: Transfers/Pending
        public async Task<IActionResult> Pending()
        {
            var transfers = await _context.Transfers
                .Include(t => t.Material)
                .Include(t => t.FromLocation)
                .Include(t => t.ToLocation)
                .Where(t => !t.IsConfirmed && !t.IsCancelled)
                .OrderBy(t => t.TransferDate)
                .ToListAsync();

            return View(transfers);
        }

        // GET: Transfers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var transfer = await _context.Transfers
                .Include(t => t.Material)
                .Include(t => t.FromLocation)
                .Include(t => t.ToLocation)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (transfer == null) return NotFound();

            if (transfer.IsExecuted)
            {
                TempData["Error"] = "لا يمكن حذف نقل تم تنفيذه";
                return RedirectToAction(nameof(Index));
            }

            return View(transfer);
        }

        // POST: Transfers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var transfer = await _context.Transfers.FindAsync(id);
            if (transfer == null) return NotFound();

            if (transfer.IsExecuted)
            {
                TempData["Error"] = "لا يمكن حذف نقل تم تنفيذه";
                return RedirectToAction(nameof(Index));
            }

            _context.Transfers.Remove(transfer);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم حذف طلب النقل";
            return RedirectToAction(nameof(Index));
        }

        private async Task LoadSelectLists(int? materialId, int? fromLocationId)
        {
            ViewBag.Materials = await _context.Materials
                .Where(m => m.IsActive)
                .OrderBy(m => m.Name)
                .ToListAsync();

            ViewBag.Locations = await _context.Locations
                .Where(l => l.IsActive)
                .OrderBy(l => l.Name)
                .ToListAsync();

            // إذا تم تحديد المادة، جلب المواقع التي تحتوي على مخزون
            if (materialId.HasValue)
            {
                ViewBag.LocationsWithStock = await _context.MaterialStocks
                    .Include(s => s.Location)
                    .Where(s => s.MaterialId == materialId.Value && s.Quantity > 0)
                    .Select(s => new { s.LocationId, s.Location.Name, s.Quantity })
                    .ToListAsync();
            }
        }
    }
}
