using System;
using System.Collections.Generic;
using System.Linq;
using WarehouseManagement.Models.ViewModels;

public class InventoryForm2ViewModel
{
    public int Year { get; set; }
    public DateTime ReportDate { get; set; }
    public DateTime InventoryDate { get; set; }

    public string MinistryName { get; set; } = string.Empty;
    public string InstitutionName { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public string WarehouseManager { get; set; } = string.Empty;
    public string? CommitteeNotes { get; set; }

    // ✅ تغيير النوع هنا
    public List<InventoryForm2ItemViewModel> Items { get; set; }

    public InventoryForm2ViewModel()
    {
        Items = new List<InventoryForm2ItemViewModel>();
        ReportDate = DateTime.Today;
        InventoryDate = DateTime.Today;
    }

    public int TotalItems => Items?.Count ?? 0;
    public int MatchingCount => Items?.Count(i => i.ActualQuantity == i.BookQuantity) ?? 0;
    public int ShortageCount => Items?.Count(i => i.ActualQuantity < i.BookQuantity) ?? 0;
    public int SurplusCount => Items?.Count(i => i.ActualQuantity > i.BookQuantity) ?? 0;

    public string FormNumber { get; set; } = string.Empty;
    public string WarehouseKeeper { get; set; } = string.Empty;
    public string CommitteeHead { get; set; } = string.Empty;
    public int? CategoryId { get; set; }
    public int? LocationId { get; set; }
}