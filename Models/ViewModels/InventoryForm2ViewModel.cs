using System;
using System.Collections.Generic;
using System.Linq;

namespace WarehouseManagement.Models.ViewModels
{
    /// <summary>
    /// نموذج رقم (2) - الجرد السنوي
    /// </summary>
    public class InventoryForm2ViewModel
    {
        public int Year { get; set; }
        public DateTime ReportDate { get; set; } = DateTime.Today;
        public DateTime InventoryDate { get; set; } = DateTime.Today;

        public string MinistryName { get; set; } = string.Empty;
        public string InstitutionName { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public string WarehouseManager { get; set; } = string.Empty;
        public string WarehouseKeeper { get; set; } = string.Empty;
        public string CommitteeHead { get; set; } = string.Empty;
        public string? CommitteeNotes { get; set; }
        public string? DirectorDecision { get; set; }
        public string FormNumber { get; set; } = string.Empty;

        public int? CategoryId { get; set; }
        public int? LocationId { get; set; }

        public List<InventoryForm2ItemViewModel> Items { get; set; } = new List<InventoryForm2ItemViewModel>();

        // Statistics
        public int TotalItems => Items?.Count ?? 0;
        public decimal TotalQuantity => Items?.Sum(i => i.ActualQuantity) ?? 0;
        public decimal TotalValue => Items?.Sum(i => i.ActualValue) ?? 0;
        public int MatchingCount => Items?.Count(i => i.ActualQuantity == i.BookQuantity) ?? 0;
        public int ShortageCount => Items?.Count(i => i.ActualQuantity < i.BookQuantity) ?? 0;
        public int SurplusCount => Items?.Count(i => i.ActualQuantity > i.BookQuantity) ?? 0;
    }

    public class InventoryForm2ItemViewModel
    {
        public string MaterialCode { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public decimal BookQuantity { get; set; }
        public decimal BookValue { get; set; }
        public decimal ActualQuantity { get; set; }
        public decimal ActualValue { get; set; }
        public decimal Variance => ActualQuantity - BookQuantity;
        public decimal VarianceValue => ActualValue - BookValue;
        public string? Notes { get; set; }
    }
}
