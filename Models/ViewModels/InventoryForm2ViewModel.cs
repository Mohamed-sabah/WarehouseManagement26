using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// ViewModel for Inventory Form 2 Razor view.
/// Placed in global namespace so the Razor file's `@model InventoryForm2ViewModel` resolves
/// without requiring additional using directives. Move into a namespace if your project uses one
/// and update the Razor `@model` accordingly (e.g. `@model YourApp.ViewModels.InventoryForm2ViewModel`).
/// </summary>
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

    public List<ItemViewModel> Items { get; set; }

    public InventoryForm2ViewModel()
    {
        Items = new List<ItemViewModel>();
        ReportDate = DateTime.Today;
        InventoryDate = DateTime.Today;
    }

    // convenience helpers used by the Razor view (optional but useful)
    public int TotalItems => Items?.Count ?? 0;
    public int MatchingCount => Items?.Count(i => i.ActualQuantity == i.RecordedQuantity) ?? 0;
    public int ShortageCount => Items?.Count(i => i.ActualQuantity < i.RecordedQuantity) ?? 0;
    public int SurplusCount => Items?.Count(i => i.ActualQuantity > i.RecordedQuantity) ?? 0;
}

public class ItemViewModel
{
    public string CategoryName { get; set; } = string.Empty;
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal RecordedQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal ActualQuantity { get; set; }
    public string? Notes { get; set; }
}