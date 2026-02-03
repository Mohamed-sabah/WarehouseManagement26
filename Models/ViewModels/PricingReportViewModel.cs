using System;
using System.Collections.Generic;
using WarehouseManagement.Models;

namespace WarehouseManagement.Models.ViewModels
{
    public class PricingReportViewModel
    {
        public string InstitutionName { get; set; } = string.Empty;
        public int? CategoryId { get; set; }
        public string? PriceChangeFilter { get; set; }
        public string? SortBy { get; set; }
        public List<PricingReportItemViewModel> Items { get; set; } = new();
        // Detail properties used by controller when showing a single material
        public Material? Material { get; set; }
        public List<Purchase>? Purchases { get; set; }
        public int TotalQuantityPurchased { get; set; }
        public decimal TotalAmountSpent { get; set; }
        public decimal AveragePrice { get; set; }
        public decimal CurrentValue { get; set; }
        public DateTime? FirstPurchaseDate { get; set; }
        public DateTime? LastPurchaseDate { get; set; }
    }
    public class PricingReportItemViewModel
    {
        public int MaterialId { get; set; }
        public string MaterialCode { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal PreviousPrice { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal PriceChange => CurrentPrice - PreviousPrice;
        public double PriceChangePercent => PreviousPrice > 0 ? (double)(PriceChange / PreviousPrice * 100) : 0;
        public DateTime LastUpdated { get; set; }
    }
}
