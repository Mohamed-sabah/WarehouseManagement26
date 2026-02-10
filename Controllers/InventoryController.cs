using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseManagement.Data;
using WarehouseManagement.Models;
using WarehouseManagement.Models.ViewModels;

namespace WarehouseManagement.Controllers
{
    public class InventoryController : Controller
    {
        private readonly WarehouseContext _context;

        public InventoryController(WarehouseContext context)
        {
            _context = context;
        }

        // GET: Inventory
        public async Task<IActionResult> Index(int? year, int? locationId, string? department, string? searchTerm)
        {
            year ??= DateTime.Now.Year;

            var query = _context.InventoryRecords
                .Include(i => i.Material)
                    .ThenInclude(m => m.Category)
                .Include(i => i.Location)
                .Include(i => i.Consumptions)
                .Where(i => i.Year == year)
                .AsQueryable();

            if (locationId.HasValue)
                query = query.Where(i => i.LocationId == locationId.Value);

            if (!string.IsNullOrEmpty(department))
                query = query.Where(i => i.Department == department);

            if (!string.IsNullOrEmpty(searchTerm))
                query = query.Where(i => i.Material.Name.Contains(searchTerm) ||
                                        i.Material.Code.Contains(searchTerm));

            var records = await query.OrderBy(i => i.Material.Name).ToListAsync();

            var viewModel = new InventoryListViewModel
            {
                Year = year.Value,
                LocationId = locationId,
                Department = department,
                SearchTerm = searchTerm,
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
                    ItemsMatching = records.Count(r => r.IsMatching),
                    ItemsWithConsumption = records.Count(r => r.HasConsumptions)
                },
                Locations = await _context.Locations.Where(l => l.IsActive).OrderBy(l => l.Name).ToListAsync(),
                Departments = await _context.InventoryRecords.Select(i => i.Department).Distinct().Where(d => d != null).ToListAsync(),
                AvailableYears = await _context.InventoryRecords.Select(i => i.Year).Distinct().OrderByDescending(y => y).ToListAsync()
            };

            return View(viewModel);
        }

        // GET: Inventory/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var record = await _context.InventoryRecords
                .Include(i => i.Material)
                    .ThenInclude(m => m.Category)
                .Include(i => i.Material)
                    .ThenInclude(m => m.Purchases)
                .Include(i => i.Location)
                .Include(i => i.Consumptions)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (record == null) return NotFound();

            return View(record);
        }

        // GET: Inventory/Create
        public async Task<IActionResult> Create()
        {
            var viewModel = new InventoryCreateEditViewModel
            {
                Record = new InventoryRecord
                {
                    Year = DateTime.Now.Year,
                    InventoryDate = DateTime.Now,
                    Condition = "جيدة"
                },
                Materials = await _context.Materials.Where(m => m.IsActive).OrderBy(m => m.Name).ToListAsync(),
                Locations = await _context.Locations.Where(l => l.IsActive).OrderBy(l => l.Name).ToListAsync()
            };

            return View(viewModel);
        }

        // POST: Inventory/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(InventoryCreateEditViewModel viewModel)
        {
            var exists = await _context.InventoryRecords
                .AnyAsync(i => i.MaterialId == viewModel.Record.MaterialId &&
                              i.LocationId == viewModel.Record.LocationId &&
                              i.Year == viewModel.Record.Year);

            if (exists)
            {
                ModelState.AddModelError("", "يوجد سجل جرد لهذه المادة في هذا الموقع لنفس السنة");
                await LoadSelectLists(viewModel);
                return View(viewModel);
            }

            var stock = await _context.MaterialStocks
                .FirstOrDefaultAsync(s => s.MaterialId == viewModel.Record.MaterialId &&
                                         s.LocationId == viewModel.Record.LocationId);

            viewModel.Record.RecordedQuantity = stock?.Quantity ?? 0;
            viewModel.Record.CreatedDate = DateTime.Now;
            viewModel.Record.StoredDifference = viewModel.Record.Difference;

            _context.InventoryRecords.Add(viewModel.Record);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم إضافة سجل الجرد بنجاح";
            return RedirectToAction(nameof(Details), new { id = viewModel.Record.Id });
        }

        // GET: Inventory/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var record = await _context.InventoryRecords.FindAsync(id);
            if (record == null) return NotFound();

            var viewModel = new InventoryCreateEditViewModel
            {
                Record = record,
                Materials = await _context.Materials.Where(m => m.IsActive).OrderBy(m => m.Name).ToListAsync(),
                Locations = await _context.Locations.Where(l => l.IsActive).OrderBy(l => l.Name).ToListAsync()
            };

            return View(viewModel);
        }

        // POST: Inventory/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, InventoryCreateEditViewModel viewModel)
        {
            if (id != viewModel.Record.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(viewModel.Record);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "تم تحديث سجل الجرد بنجاح";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.InventoryRecords.AnyAsync(i => i.Id == id))
                        return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Details), new { id });
            }

            await LoadSelectLists(viewModel);
            return View(viewModel);
        }

        // GET: Inventory/StartBulkInventory - الصفحة المفقودة
        public async Task<IActionResult> StartBulkInventory()
        {
            ViewBag.Locations = await _context.Locations.Where(l => l.IsActive).OrderBy(l => l.Name).ToListAsync();
            return View("BulkCreate", new BulkInventoryViewModel
            {
                Year = DateTime.Now.Year,
                InventoryDate = DateTime.Now
            });
        }

        // GET: Inventory/BulkCreate
        public async Task<IActionResult> BulkCreate(int? locationId)
        {
            var viewModel = new BulkInventoryViewModel
            {
                Year = DateTime.Now.Year,
                InventoryDate = DateTime.Now,
                LocationId = locationId
            };

            ViewBag.Locations = await _context.Locations.Where(l => l.IsActive).OrderBy(l => l.Name).ToListAsync();

            if (locationId.HasValue)
            {
                var stocks = await _context.MaterialStocks
                    .Include(s => s.Material)
                        .ThenInclude(m => m.Category)
                    .Where(s => s.LocationId == locationId.Value && s.Quantity > 0)
                    .ToListAsync();

                viewModel.Items = stocks.Select(s => new BulkInventoryItemViewModel
                {
                    MaterialId = s.MaterialId,
                    MaterialName = s.Material.Name,
                    MaterialCode = s.Material.Code,
                    RecordedQuantity = s.Quantity,
                    ActualQuantity = s.Quantity,
                    Condition = MaterialCondition.Good
                }).ToList();
            }

            return View(viewModel);
        }

        // POST: Inventory/BulkCreate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkCreate(BulkInventoryViewModel viewModel)
        {
            if (!viewModel.LocationId.HasValue)
            {
                ModelState.AddModelError("LocationId", "يرجى اختيار الموقع");
                ViewBag.Locations = await _context.Locations.Where(l => l.IsActive).ToListAsync();
                return View(viewModel);
            }

            int addedCount = 0;

            foreach (var item in viewModel.Items.Where(i => i.IsSelected))
            {
                var exists = await _context.InventoryRecords
                    .AnyAsync(i => i.MaterialId == item.MaterialId &&
                                  i.LocationId == viewModel.LocationId &&
                                  i.Year == viewModel.Year);

                if (exists) continue;

                var record = new InventoryRecord
                {
                    MaterialId = item.MaterialId,
                    LocationId = viewModel.LocationId.Value,
                    Year = viewModel.Year,
                    InventoryDate = viewModel.InventoryDate,
                    RecordedQuantity = item.RecordedQuantity,
                    ActualQuantity = item.ActualQuantity,
                    Condition = item.Condition.ToString(),
                    Department = viewModel.Department,
                    Notes = item.Notes,
                    StoredDifference = item.Difference,
                    CreatedDate = DateTime.Now
                };

                _context.InventoryRecords.Add(record);
                addedCount++;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"تم إنشاء {addedCount} سجل جرد بنجاح";
            return RedirectToAction(nameof(Index), new { year = viewModel.Year, locationId = viewModel.LocationId });
        }

        // GET: Inventory/Report
        public async Task<IActionResult> Report(int? year, int? locationId, string? department)
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

            return View(viewModel);
        }

        // GET: Inventory/TransferToConsumption/5
        public async Task<IActionResult> TransferToConsumption(int? id)
        {
            if (id == null) return NotFound();

            var record = await _context.InventoryRecords
                .Include(i => i.Material)
                .Include(i => i.Location)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (record == null) return NotFound();

            var viewModel = new TransferToConsumptionViewModel
            {
                InventoryRecordId = record.Id,
                InventoryRecord = record,
                Reason = ConsumptionReason.EndOfLife,
                Decision = CommitteeDecision.Dispose
            };

            return View(viewModel);
        }

        // POST: Inventory/TransferToConsumption
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TransferToConsumption(TransferToConsumptionViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var inventory = await _context.InventoryRecords
                    .Include(i => i.Material)
                        .ThenInclude(m => m.Purchases)
                    .FirstOrDefaultAsync(i => i.Id == viewModel.InventoryRecordId);

                if (inventory == null) return NotFound();

                var consumption = new ConsumptionRecord
                {
                    InventoryRecordId = viewModel.InventoryRecordId,
                    ConsumedQuantity = viewModel.ConsumedQuantity,
                    Reason = viewModel.Reason,
                    ReasonDetails = viewModel.ReasonDetails,
                    ReasonDescription = viewModel.ReasonDetails,
                    DamagePercentage = viewModel.DamagePercentage,
                    Decision = viewModel.Decision,
                    DecisionDetails = viewModel.DecisionDetails,
                    DecisionNotes = viewModel.DecisionDetails,
                    ReportDate = DateTime.Now,
                    OriginalUnitPrice = inventory.Material.AveragePrice,
                    StoredOriginalValue = viewModel.ConsumedQuantity * inventory.Material.AveragePrice,
                    StoredResidualValue = viewModel.ResidualValue ?? 0,
                    CreatedDate = DateTime.Now
                };

                _context.ConsumptionRecords.Add(consumption);
                await _context.SaveChangesAsync();

                TempData["Success"] = "تم تحويل السجل للاستهلاك بنجاح";
                return RedirectToAction("Details", "Consumption", new { id = consumption.Id });
            }

            viewModel.InventoryRecord = await _context.InventoryRecords
                .Include(i => i.Material)
                .Include(i => i.Location)
                .FirstOrDefaultAsync(i => i.Id == viewModel.InventoryRecordId);

            return View(viewModel);
        }

        // GET: Inventory/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var record = await _context.InventoryRecords
                .Include(i => i.Material)
                .Include(i => i.Location)
                .Include(i => i.Consumptions)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (record == null) return NotFound();

            return View(record);
        }

        // POST: Inventory/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var record = await _context.InventoryRecords
                .Include(i => i.Consumptions)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (record == null) return NotFound();

            if (record.Consumptions.Any())
            {
                TempData["Error"] = "لا يمكن حذف سجل جرد يحتوي على سجلات استهلاك";
                return RedirectToAction(nameof(Delete), new { id });
            }

            _context.InventoryRecords.Remove(record);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تم حذف سجل الجرد بنجاح";
            return RedirectToAction(nameof(Index));
        }

        private async Task LoadSelectLists(InventoryCreateEditViewModel viewModel)
        {
            viewModel.Materials = await _context.Materials.Where(m => m.IsActive).OrderBy(m => m.Name).ToListAsync();
            viewModel.Locations = await _context.Locations.Where(l => l.IsActive).OrderBy(l => l.Name).ToListAsync();
        }
    }
}