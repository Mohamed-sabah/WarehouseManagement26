using System;

namespace WarehouseManagement.Models.ViewModels
{
    public class InventoryReportItemViewModel
    {
        public string MaterialCode { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;
        public string? CategoryName { get; set; }
        // Values expected by Views/Inventory/Report.cshtml
        public string Unit { get; set; } = string.Empty;
        public int RecordedQuantity { get; set; }
        public int ActualQuantity { get; set; }
        public decimal UnitPrice { get; set; }
        public int Difference { get; set; }
        public decimal CurrentValue { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public MaterialCondition Condition { get; set; } = MaterialCondition.Good;
        public string? Notes { get; set; }
    }
}
