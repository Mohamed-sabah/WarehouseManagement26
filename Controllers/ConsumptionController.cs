using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Data;
using WarehouseManagement.Models;
using WarehouseManagement.Models.ViewModels;

namespace WarehouseManagement.Controllers
{
    public class ConsumptionController : Controller
    {
        private readonly WarehouseContext _context;

        public ConsumptionController(WarehouseContext context)
        {
            _context = context;
        }

        // GET: Consumption
        public async Task<IActionResult> Index(int? year, int? month, int? categoryId, string? searchTerm,
            ConsumptionReason? reason, CommitteeDecision? decision)
        {
            year ??= DateTime.Now.Year;

            var query = _context.ConsumptionRecords
                .Include(c => c.InventoryRecord)
                    .ThenInclude(i => i.Material)
                        .ThenInclude(m => m.Category)
                .Include(c => c.InventoryRecord)
                    .ThenInclude(i => i.Location)
                .Where(c => c.ReportDate.Year == year)
                .AsQueryable();

            if (month.HasValue)
                query = query.Where(c => c.ReportDate.Month == month.Value);

            if (categoryId.HasValue)
                query = query.Where(c => c.InventoryRecord.Material.CategoryId == categoryId.Value);

            if (!string.IsNullOrEmpty(searchTerm))
                query = query.Where(c => c.InventoryRecord.Material.Name.Contains(searchTerm) ||
                                        c.InventoryRecord.Material.Code.Contains(searchTerm));

            if (reason.HasValue)
                query = query.Where(c => c.Reason == reason.Value);

            if (decision.HasValue)
                query = query.Where(c => c.Decision == decision.Value);

            var records = await query.OrderByDescending(c => c.ReportDate).ToListAsync();

            // تحديد ما إذا كان السجل تم التصرف فيه بناءً على DisposalDate
            var disposedCount = records.Count(r => r.DisposalDate.HasValue);
            var pendingCount = records.Count(r => !r.DisposalDate.HasValue);

            var viewModel = new ConsumptionListViewModel
            {
                YearFilter = year,
                SearchTerm = searchTerm,
                ReasonFilter = reason,
                DecisionFilter = decision,
                Records = records,
                Statistics = new ConsumptionStatistics
                {
                    TotalItems = records.Count,
                    TotalQuantity = records.Sum(r => r.ConsumedQuantity),
                    TotalOriginalValue = records.Sum(r => r.OriginalValue),
                    TotalResidualValue = records.Sum(r => r.ResidualValue),
                    ItemsDisposed = disposedCount,
                    ItemsPending = pendingCount
                },
                AvailableYears = await _context.ConsumptionRecords
                    .Select(c => c.ReportDate.Year)
                    .Distinct()
                    .OrderByDescending(y => y)
                    .ToListAsync(),
                Departments = await _context.InventoryRecords
                    .Where(i => i.Department != null)
                    .Select(i => i.Department!)
                    .Distinct()
                    .ToListAsync()
            };

            ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();

            return View(viewModel);
        }

        // GET: Consumption/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var record = await _context.ConsumptionRecords
                .Include(c => c.InventoryRecord)
                    .ThenInclude(i => i.Material)
                        .ThenInclude(m => m.Category)
                .Include(c => c.InventoryRecord)
                    .ThenInclude(i => i.Location)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (record == null) return NotFound();

            return View(record);
        }

        // GET: Consumption/Create
        public async Task<IActionResult> Create(int? inventoryId)
        {
            var viewModel = new ConsumptionCreateEditViewModel
            {
                Record = new ConsumptionRecord
                {
                    ReportDate = DateTime.Now
                },
                SelectedInventoryRecordId = inventoryId
            };

            if (inventoryId.HasValue)
            {
                var inventory = await _context.InventoryRecords
                    .Include(i => i.Material)
                    .Include(i => i.Location)
                    .FirstOrDefaultAsync(i => i.Id == inventoryId);

                if (inventory != null)
                {
                    viewModel.Record.InventoryRecordId = inventory.Id;
                    viewModel.Record.InventoryRecord = inventory;
                    viewModel.Record.OriginalUnitPrice = inventory.Material.AveragePrice;
                }
            }

            viewModel.AvailableInventoryRecords = await _context.InventoryRecords
                .Include(i => i.Material)
                .Include(i => i.Location)
                .Where(i => i.ActualQuantity > 0)
                .Select(i => new InventoryRecordSelectViewModel
                {
                    Id = i.Id,
                    MaterialName = i.Material.Name,
                    MaterialCode = i.Material.Code,
                    AvailableQuantity = i.ActualQuantity,
                    TotalCost = i.TotalCost,
                    Department = i.Department,
                    Year = i.Year
                })
                .OrderBy(i => i.MaterialName)
                .ToListAsync();

            return View(viewModel);
        }

        // POST: Consumption/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ConsumptionCreateEditViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var inventory = await _context.InventoryRecords
                    .Include(i => i.Material)
                    .FirstOrDefaultAsync(i => i.Id == viewModel.Record.InventoryRecordId);

                if (inventory != null)
                {
                    viewModel.Record.OriginalUnitPrice = inventory.Material.AveragePrice;
                    viewModel.Record.StoredOriginalValue = viewModel.Record.ConsumedQuantity * viewModel.Record.OriginalUnitPrice;
                }

                viewModel.Record.CreatedDate = DateTime.Now;
                _context.ConsumptionRecords.Add(viewModel.Record);
                await _context.SaveChangesAsync();

                TempData["Success"] = "تم إنشاء سجل الاستهلاك بنجاح";
                return RedirectToAction(nameof(Details), new { id = viewModel.Record.Id });
            }

            viewModel.AvailableInventoryRecords = await _context.InventoryRecords
                .Include(i => i.Material)
                .Where(i => i.ActualQuantity > 0)
                .Select(i => new InventoryRecordSelectViewModel
                {
                    Id = i.Id,
                    MaterialName = i.Material.Name,
                    MaterialCode = i.Material.Code,
                    AvailableQuantity = i.ActualQuantity
                })
                .ToListAsync();

            return View(viewModel);
        }

        // GET: Consumption/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var record = await _context.ConsumptionRecords
                .Include(c => c.InventoryRecord)
                    .ThenInclude(i => i.Material)
                        .ThenInclude(m => m.Category)
                .Include(c => c.InventoryRecord)
                    .ThenInclude(i => i.Location)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (record == null) return NotFound();

            return View(record);
        }

        // POST: Consumption/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ConsumptionRecord record)
        {
            if (id != record.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    record.StoredOriginalValue = record.ConsumedQuantity * record.OriginalUnitPrice;
                    _context.Update(record);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "تم تحديث سجل الاستهلاك بنجاح";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.ConsumptionRecords.AnyAsync(c => c.Id == id))
                        return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Details), new { id });
            }

            record.InventoryRecord = await _context.InventoryRecords
                .Include(i => i.Material)
                .Include(i => i.Location)
                .FirstOrDefaultAsync(i => i.Id == record.InventoryRecordId);

            return View(record);
        }

        // GET: Consumption/ProcessDecision/5
        public async Task<IActionResult> ProcessDecision(int? id)
        {
            if (id == null) return NotFound();

            var record = await _context.ConsumptionRecords
                .Include(c => c.InventoryRecord)
                    .ThenInclude(i => i.Material)
                        .ThenInclude(m => m.Category)
                .Include(c => c.InventoryRecord)
                    .ThenInclude(i => i.Location)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (record == null) return NotFound();

            var viewModel = new ProcessDecisionViewModel
            {
                ConsumptionRecordId = record.Id,
                Record = record,
                Action = DisposalAction.Dispose
            };

            return View(viewModel);
        }

        // POST: Consumption/ProcessDecision/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessDecision(ProcessDecisionViewModel viewModel)
        {
            var record = await _context.ConsumptionRecords
                .Include(c => c.InventoryRecord)
                .FirstOrDefaultAsync(c => c.Id == viewModel.ConsumptionRecordId);

            if (record == null) return NotFound();

            // تحديث الملاحظات
            var currentNotes = record.Notes ?? "";
            currentNotes += $"\n[معالجة {DateTime.Now:yyyy/MM/dd}] {viewModel.Action}: {viewModel.Notes}";

            if (!string.IsNullOrEmpty(viewModel.DisposalRecordNumber))
                currentNotes += $"\nرقم المحضر: {viewModel.DisposalRecordNumber}";

            record.Notes = currentNotes;

            switch (viewModel.Action)
            {
                case DisposalAction.Dispose:
                    record.DisposalMethod = viewModel.DisposalMethod;
                    record.DisposalDate = DateTime.Now;
                    break;

                case DisposalAction.Revert:
                    if (record.InventoryRecord != null)
                    {
                        record.InventoryRecord.ActualQuantity += record.ConsumedQuantity;
                        record.Notes += "\nتم إعادة الكمية للمخزون";
                    }
                    record.DisposalDate = null;
                    break;

                case DisposalAction.UpdateDecision:
                    record.Notes += $"\nتم تحديث القرار";
                    break;
            }

            if (viewModel.SaleValue.HasValue)
                record.StoredResidualValue = viewModel.SaleValue.Value;

            await _context.SaveChangesAsync();
            TempData["Success"] = "تم معالجة القرار بنجاح";
            return RedirectToAction(nameof(Details), new { id = viewModel.ConsumptionRecordId });
        }

        // GET: Consumption/Report
        public async Task<IActionResult> Report(DateTime? startDate, DateTime? endDate, int? categoryId)
        {
            startDate ??= new DateTime(DateTime.Now.Year, 1, 1);
            endDate ??= DateTime.Now;

            var query = _context.ConsumptionRecords
                .Include(c => c.InventoryRecord)
                    .ThenInclude(i => i.Material)
                        .ThenInclude(m => m.Category)
                .Include(c => c.InventoryRecord)
                    .ThenInclude(i => i.Location)
                .Where(c => c.ReportDate >= startDate && c.ReportDate <= endDate);

            if (categoryId.HasValue)
                query = query.Where(c => c.InventoryRecord.Material.CategoryId == categoryId.Value);

            var records = await query.OrderBy(c => c.ReportDate).ToListAsync();

            var byReason = records.GroupBy(r => r.Reason)
                .ToDictionary(g => g.Key, g => g.Count());

            var byDecision = records.GroupBy(r => r.Decision)
                .ToDictionary(g => g.Key, g => g.Count());

            // تحديد ما إذا كان السجل تم التصرف فيه بناءً على DisposalDate
            var disposedCount = records.Count(r => r.DisposalDate.HasValue);
            var pendingCount = records.Count(r => !r.DisposalDate.HasValue);

            var viewModel = new ConsumptionReportViewModel
            {
                StartDate = startDate.Value,
                EndDate = endDate.Value,
                CategoryId = categoryId,
                InstitutionName = "المؤسسة",
                ReportTitle = "نموذج رقم (5) - قائمة بالموجودات المستهلكة",
                ReportDate = DateTime.Now,
                Records = records,
                Statistics = new ConsumptionStatistics
                {
                    TotalItems = records.Count,
                    TotalQuantity = records.Sum(r => r.ConsumedQuantity),
                    TotalOriginalValue = records.Sum(r => r.OriginalValue),
                    TotalResidualValue = records.Sum(r => r.ResidualValue),
                    ItemsDisposed = disposedCount,
                    ItemsPending = pendingCount,
                    ByReason = byReason,
                    ByDecision = byDecision,
                    GeneratedDate = DateTime.Now
                }
            };

            ViewBag.Categories = await _context.Categories.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync();

            return View(viewModel);
        }

        // GET: Consumption/ExportReport
        public async Task<IActionResult> ExportReport(int year, int? month, int? categoryId)
        {
            TempData["Info"] = "جاري تطوير ميزة التصدير";
            return RedirectToAction(nameof(Report), new { startDate = new DateTime(year, 1, 1), endDate = new DateTime(year, 12, 31), categoryId });
        }

        // GET: Consumption/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var record = await _context.ConsumptionRecords
                .Include(c => c.InventoryRecord)
                    .ThenInclude(i => i.Material)
                .Include(c => c.InventoryRecord)
                    .ThenInclude(i => i.Location)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (record == null) return NotFound();

            return View(record);
        }

        // POST: Consumption/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var record = await _context.ConsumptionRecords.FindAsync(id);
            if (record == null) return NotFound();

            // التحقق من أن السجل لم يتم التصرف فيه بعد
            if (record.DisposalDate.HasValue)
            {
                TempData["Error"] = "لا يمكن حذف سجل تم التصرف فيه";
                return RedirectToAction(nameof(Delete), new { id });
            }

            _context.ConsumptionRecords.Remove(record);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم حذف سجل الاستهلاك بنجاح";
            return RedirectToAction(nameof(Index));
        }
    }
}