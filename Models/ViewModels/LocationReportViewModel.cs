using System;
using System.Collections.Generic;

namespace WarehouseManagement.Models.ViewModels
{
    public class LocationsSummaryReportViewModel
    {
        public string InstitutionName { get; set; } = string.Empty;
        public List<LocationsSummaryItemViewModel> Locations { get; set; } = new();
    }

    public class LocationsSummaryItemViewModel
    {
        public int LocationId { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public string LocationCode { get; set; } = string.Empty;
        public string? ManagerName { get; set; }
        public int MaterialsCount { get; set; }
        public decimal TotalQuantity { get; set; }
        public decimal TotalValue { get; set; }
        public int LowStockCount { get; set; }
        public bool IsActive { get; set; }
        public List<TopMaterialViewModel>? TopMaterials { get; set; }
    }
    
    public class TopMaterialViewModel
    {
        public string MaterialName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Value { get; set; }
    }

    // Detailed report type used by controllers (single-location report)
    public class LocationReportViewModel
    {
        public Location Location { get; set; } = null!;
        public List<MaterialStock> Stocks { get; set; } = new();
        public int TotalItems { get; set; }
        public int TotalQuantity { get; set; }
        public decimal TotalValue { get; set; }
        public DateTime GeneratedDate { get; set; } = DateTime.Now;

        // Also include summary collection for the locations-summary view
        public List<LocationsSummaryItemViewModel>? Locations { get; set; }
        public string InstitutionName { get; set; } = string.Empty;
    }
}
