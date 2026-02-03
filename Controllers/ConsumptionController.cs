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
        public async Task<IActionResult> Index(int? year, ConsumptionReason? reason, CommitteeDecision? decision, string? department, bool? isDisposed)
        {
            var query = _context.ConsumptionRecords
                .Include(c => c.InventoryRecord)
                    .ThenInclude(i => i.Material)
                        .ThenInclude(m => m.Category)
                .Include(c => c.InventoryRecord)
                    .ThenInclude(i => i.Location)
                .AsQueryable();

            if (year.HasValue)
                query = query.Where(c => c.ReportDate.Year == year.Value);

            if (reason.HasValue)
                query = query.Where(c => c.Reason == reason.Value);

            if (decision.HasValue)
                query = query.Where(c => c.Decision == decision.Value);

            if (!string.IsNullOrEmpty(department))
                query = query.Where(c => c.InventoryRecord.Department == department);

            if (isDisposed.HasValue)
                query = query.Where(c => c.IsDisposed == isDisposed.Value);

            var records = await query.OrderByDescending(c => c.ReportDate).ToListAsync();

            var viewModel = new ConsumptionListViewModel
            {
                ReasonFilter = reason,
                DecisionFilter = decision,
                Department = department,
                YearFilter = year,
                IsDisposedFilter = isDisposed,
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
                },
                Departments = await _context.InventoryRecords.Select(i => i.Department).Distinct().Where(d => d != null).ToListAsync()!,
                AvailableYears = await _context.ConsumptionRecords.Select(c => c.ReportDate.Year).Distinct().OrderByDescending(y => y).ToListAsync()
            };

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
        public async Task<IActionResult> Create(int? inventoryRecordId)
        {
            var viewModel = new ConsumptionCreateEditViewModel
            {
                Record = new ConsumptionRecord
                {
                    ReportDate = DateTime.Now,
                    Reason = ConsumptionReason.EndOfUsefulLife,
                    Decision = CommitteeDecision.Disposal
                },
                SelectedInventoryRecordId = inventoryRecordId,
                AvailableInventoryRecords = await GetAvailableInventoryRecords()
            };

            if (inventoryRecordId.HasValue)
            {
                var inventory = await _context.InventoryRecords
                    .Include(i => i.Material)
                    .FirstOrDefaultAsync(i => i.Id == inventoryRecordId.Value);

                if (inventory != null)
                {
                    viewModel.Record.InventoryRecordId = inventory.Id;
                    viewModel.Record.ConsumedQuantity = inventory.ActualQuantity;
                    viewModel.Record.OriginalUnitPrice = inventory.UnitPrice;
                }
            }

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
                        .ThenInclude(m => m.Purchases)
                    .FirstOrDefaultAsync(i => i.Id == viewModel.Record.InventoryRecordId);

                if (inventory == null)
                {
                    ModelState.AddModelError("Record.InventoryRecordId", "سجل الجرد غير موجود");
                    viewModel.AvailableInventoryRecords = await GetAvailableInventoryRecords();
                    return View(viewModel);
                }

                viewModel.Record.OriginalUnitPrice = inventory.Material.AveragePrice;
                viewModel.Record.StoredOriginalValue = viewModel.Record.ConsumedQuantity * viewModel.Record.OriginalUnitPrice;
                viewModel.Record.CreatedDate = DateTime.Now;

                _context.ConsumptionRecords.Add(viewModel.Record);

                // خصم من المخزون إذا طلب
                if (viewModel.DeductFromStockAutomatically)
                {
                    var stock = await _context.MaterialStocks
                        .FirstOrDefaultAsync(s => s.MaterialId == inventory.MaterialId &&
                                                 s.LocationId == inventory.LocationId);

                    if (stock != null && stock.Quantity >= viewModel.Record.ConsumedQuantity)
                    {
                        stock.Quantity -= viewModel.Record.ConsumedQuantity;
                        stock.LastUpdated = DateTime.Now;
                    }
                }

                await _context.SaveChangesAsync();

                TempData["Success"] = "تم إضافة سجل الاستهلاك بنجاح";
                return RedirectToAction(nameof(Details), new { id = viewModel.Record.Id });
            }

            viewModel.AvailableInventoryRecords = await GetAvailableInventoryRecords();
            return View(viewModel);
        }

        // GET: Consumption/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var record = await _context.ConsumptionRecords.FindAsync(id);
            if (record == null) return NotFound();

            if (record.IsDisposed)
            {
                TempData["Error"] = "لا يمكن تعديل سجل تم التصرف فيه";
                return RedirectToAction(nameof(Details), new { id });
            }

            var viewModel = new ConsumptionCreateEditViewModel
            {
                Record = record,
                AvailableInventoryRecords = await GetAvailableInventoryRecords()
            };

            return View(viewModel);
        }

        // POST: Consumption/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ConsumptionCreateEditViewModel viewModel)
        {
            if (id != viewModel.Record.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    viewModel.Record.StoredOriginalValue = viewModel.Record.ConsumedQuantity * viewModel.Record.OriginalUnitPrice;
                    _context.Update(viewModel.Record);
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

            viewModel.AvailableInventoryRecords = await GetAvailableInventoryRecords();
            return View(viewModel);
        }

        // GET: Consumption/ProcessDecision/5
        public async Task<IActionResult> ProcessDecision(int? id)
        {
            if (id == null) return NotFound();

            var record = await _context.ConsumptionRecords
                .Include(c => c.InventoryRecord)
                    .ThenInclude(i => i.Material)
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

        // POST: Consumption/ProcessDecision
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessDecision(ProcessDecisionViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var record = await _context.ConsumptionRecords.FindAsync(viewModel.ConsumptionRecordId);
                if (record == null) return NotFound();

                switch (viewModel.Action)
                {
                    case DisposalAction.Dispose:
                        record.IsDisposed = true;
                        record.DisposalDate = DateTime.Now;
                        record.DisposalRecordNumber = viewModel.DisposalRecordNumber;
                        record.SaleValue = viewModel.SaleValue;
                        record.Notes = $"{record.Notes}\n[تم التصرف] {viewModel.Notes}";
                        TempData["Success"] = "تم تنفيذ التصرف بنجاح";
                        break;

                    case DisposalAction.Revert:
                        record.IsDisposed = false;
                        record.DisposalDate = null;
                        record.DisposalRecordNumber = null;
                        record.Notes = $"{record.Notes}\n[تم الإلغاء] {viewModel.Notes}";
                        TempData["Success"] = "تم إلغاء التصرف بنجاح";
                        break;

                    case DisposalAction.UpdateDecision:
                        record.Notes = $"{record.Notes}\n[تحديث] {viewModel.Notes}";
                        TempData["Success"] = "تم تحديث السجل بنجاح";
                        break;
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Details), new { id = viewModel.ConsumptionRecordId });
            }

            viewModel.Record = await _context.ConsumptionRecords
                .Include(c => c.InventoryRecord)
                    .ThenInclude(i => i.Material)
                .FirstOrDefaultAsync(c => c.Id == viewModel.ConsumptionRecordId);

            return View(viewModel);
        }

        // GET: Consumption/Report
        public async Task<IActionResult> Report(int? year, string? department)
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
            ViewBag.AvailableYears = await _context.ConsumptionRecords.Select(c => c.ReportDate.Year).Distinct().OrderByDescending(y => y).ToListAsync();

            return View(viewModel);
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

            if (record.IsDisposed)
            {
                TempData["Error"] = "لا يمكن حذف سجل تم التصرف فيه";
                return RedirectToAction(nameof(Delete), new { id });
            }

            _context.ConsumptionRecords.Remove(record);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم حذف سجل الاستهلاك بنجاح";
            return RedirectToAction(nameof(Index));
        }

        private async Task<List<InventoryRecordSelectViewModel>> GetAvailableInventoryRecords()
        {
            var records = await _context.InventoryRecords
                .Include(i => i.Material)
                .Include(i => i.Consumptions)
                .Where(i => i.ActualQuantity > i.Consumptions.Sum(c => c.ConsumedQuantity))
                .ToListAsync();

            return records.Select(i => new InventoryRecordSelectViewModel
            {
                Id = i.Id,
                MaterialName = i.Material.Name,
                MaterialCode = i.Material.Code,
                AvailableQuantity = i.ActualQuantity - i.Consumptions.Sum(c => c.ConsumedQuantity),
                TotalCost = i.TotalCost,
                Department = i.Department,
                Year = i.Year
            }).ToList();
        }
    }
}
