using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace WarehouseManagement.Models.ViewModels
{
    #region Materials Value Report

    /// <summary>
    /// تقرير قيمة المواد
    /// </summary>
    public class MaterialsValueReportViewModel
    {
        public string InstitutionName { get; set; } = string.Empty;
        public List<MaterialsValueItemViewModel> Items { get; set; } = new();
        public decimal TotalQuantity => Items?.Sum(i => i.Quantity) ?? 0;
        public decimal TotalValue => Items?.Sum(i => i.TotalValue) ?? 0;
        public string SortBy { get; set; } = "value";
        public int? CategoryId { get; set; }
        public int? LocationId { get; set; }
    }

    public class MaterialsValueItemViewModel
    {
        public int MaterialId { get; set; }
        public string MaterialCode { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalValue { get; set; }
    }

    #endregion

    #region Pricing Report

    /// <summary>
    /// تقرير الأسعار
    /// </summary>
    public class PricingReportViewModel
    {
        public string InstitutionName { get; set; } = string.Empty;
        public List<PricingReportItemViewModel> Items { get; set; } = new();
        public int? CategoryId { get; set; }
        public decimal? MinPriceFilter { get; set; }
        public decimal? MaxPriceFilter { get; set; }
        public string SortBy { get; set; } = "name";

        public int TotalItems => Items?.Count ?? 0;
        public decimal AveragePrice => Items?.Any() == true ? Items.Average(i => i.PurchasePrice) : 0;
        public decimal HighestPrice => Items?.Any() == true ? Items.Max(i => i.PurchasePrice) : 0;
        public decimal LowestPrice => Items?.Any() == true ? Items.Min(i => i.PurchasePrice) : 0;

        public Material? Material { get; set; }
        public List<Purchase>? Purchases { get; set; }
        public int TotalQuantityPurchased { get; set; }
        public decimal TotalAmountSpent { get; set; }
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
        public decimal PurchasePrice { get; set; }
        public decimal IssuePrice { get; set; }
        public decimal LastPurchasePrice { get; set; }
        public DateTime? LastPurchaseDate { get; set; }
    }

    #endregion

    #region Low Stock Report

    public class LowStockReportViewModel
    {
        public string InstitutionName { get; set; } = string.Empty;
        public List<LowStockReportItemViewModel> Items { get; set; } = new();
        public int? CategoryId { get; set; }
        public int? LocationId { get; set; }
        public string? Status { get; set; }
        public int TotalItems => Items?.Count ?? 0;
        public int TotalShortage => Items?.Sum(i => i.ShortageQuantity) ?? 0;
    }

    public class LowStockReportItemViewModel
    {
        public int MaterialId { get; set; }
        public int StockId { get; set; }
        public string MaterialCode { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public int CurrentQuantity { get; set; }
        public int MinimumStock { get; set; }
        public int ShortageQuantity => Math.Max(0, MinimumStock - CurrentQuantity);
    }

    #endregion

    #region Expiry Report

    public class ExpiryReportViewModel
    {
        public string InstitutionName { get; set; } = string.Empty;
        public string? Period { get; set; }
        public int? CategoryId { get; set; }
        public int? LocationId { get; set; }
        public List<ExpiryReportItemViewModel> Items { get; set; } = new();

        public int TotalItems => Items?.Count ?? 0;
        public int ExpiredCount => Items?.Count(i => i.DaysRemaining <= 0) ?? 0;
        public int ExpiringThisMonthCount => Items?.Count(i => i.DaysRemaining > 0 && i.DaysRemaining <= 30) ?? 0;
        public int ExpiringIn3MonthsCount => Items?.Count(i => i.DaysRemaining > 30 && i.DaysRemaining <= 90) ?? 0;
        public decimal TotalValueAtRisk => Items?.Sum(i => i.TotalValue) ?? 0;
    }

    public class ExpiryReportItemViewModel
    {
        public int StockId { get; set; }
        public string MaterialCode { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
        public DateTime? ExpiryDate { get; set; }
        public int DaysRemaining => ExpiryDate.HasValue ? (int)(ExpiryDate.Value - DateTime.Today).TotalDays : int.MaxValue;
        public decimal UnitPrice { get; set; }
        public decimal TotalValue => Quantity * UnitPrice;
    }

    #endregion

    #region Purchases Yearly Report

    public class PurchasesYearlyReportViewModel
    {
        public int Year { get; set; }
        public string InstitutionName { get; set; } = string.Empty;
        public decimal TotalPurchases { get; set; }
        public int TotalOrders { get; set; }
        public int TotalSuppliers { get; set; }
        public decimal PreviousYearTotal { get; set; }
        public List<MonthlyPurchaseDataViewModel> MonthlyData { get; set; } = new();
        public List<CategoryPurchaseSummaryViewModel>? CategorySummary { get; set; } = new();
        public List<SupplierSummaryViewModel>? SupplierSummary { get; set; } = new();
    }

    public class MonthlyPurchaseDataViewModel
    {
        public int Month { get; set; }
        public int OrderCount { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class CategoryPurchaseSummaryViewModel
    {
        public string CategoryName { get; set; } = string.Empty;
        public int OrderCount { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class SupplierSummaryViewModel
    {
        public string SupplierName { get; set; } = string.Empty;
        public int OrderCount { get; set; }
        public decimal TotalAmount { get; set; }
    }

    #endregion

    #region Consumption Form 5 Report

    public class ConsumptionForm5ViewModel
    {
        public int Year { get; set; } = DateTime.Now.Year;
        public int Month { get; set; } = DateTime.Now.Month;
        public string FormNumber { get; set; } = string.Empty;
        public int? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public int? CategoryId { get; set; }
        public string InstitutionName { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public string WarehouseKeeper { get; set; } = string.Empty;
        public List<ConsumptionForm5ItemViewModel> Items { get; set; } = new();

        public int TotalTransactions => Items?.Count ?? 0;
        public int TotalItems => Items?.Select(i => i.MaterialCode).Distinct().Count() ?? 0;
        public decimal TotalQuantity => Items?.Sum(i => i.Quantity) ?? 0;
        public decimal TotalValue => Items?.Sum(i => i.TotalValue) ?? 0;
    }

    public class ConsumptionForm5ItemViewModel
    {
        public DateTime IssueDate { get; set; }
        public string IssueNumber { get; set; } = string.Empty;
        public string MaterialCode { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalValue => Quantity * UnitPrice;
        public string DepartmentName { get; set; } = string.Empty;
        public string ReceivedBy { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    #endregion

    #region Locations Summary Report

    public class LocationsSummaryReportViewModel
    {
        public string InstitutionName { get; set; } = string.Empty;
        public List<LocationSummaryItemViewModel> Locations { get; set; } = new();

        public int TotalLocations => Locations?.Count ?? 0;
        public int TotalMaterials => Locations?.Sum(l => l.MaterialsCount) ?? 0;
        public decimal TotalValue => Locations?.Sum(l => l.TotalValue) ?? 0;
        public int TotalLowStock => Locations?.Sum(l => l.LowStockCount) ?? 0;
    }

    public class LocationSummaryItemViewModel
    {
        public int LocationId { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public string LocationCode { get; set; } = string.Empty;
        public string? ManagerName { get; set; }
        public int MaterialsCount { get; set; }
        public decimal TotalQuantity { get; set; }
        public decimal TotalValue { get; set; }
        public int LowStockCount { get; set; }
        public bool IsActive { get; set; } = true;
        public List<TopMaterialViewModel>? TopMaterials { get; set; }
    }

    public class TopMaterialViewModel
    {
        public string MaterialName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Value { get; set; }
    }

    public class LocationReportViewModel
    {
        public string InstitutionName { get; set; } = string.Empty;
        public Location? Location { get; set; }
        public List<MaterialStock> Stocks { get; set; } = new();
        public List<LocationSummaryItemViewModel> Locations { get; set; } = new();

        // Statistics for single location
        public int TotalItems { get; set; }
        public decimal TotalQuantity { get; set; }
        public decimal TotalValue { get; set; }

        // Statistics for all locations summary
        public int TotalLocations => Locations?.Count ?? 0;
        public int TotalMaterials => Locations?.Sum(l => l.MaterialsCount) ?? 0;
        public decimal GrandTotalValue => Locations?.Sum(l => l.TotalValue) ?? 0;
        public int TotalLowStock => Locations?.Sum(l => l.LowStockCount) ?? 0;
    }

    #endregion
}
