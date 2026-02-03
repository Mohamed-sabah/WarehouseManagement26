// Plan (pseudocode):
// 1. Create a view model class named LowStockReportViewModel so the Razor view's @model resolves.
// 2. Provide properties used by the view:
//    - InstitutionName (string)
//    - Items (List<LowStockReportItemViewModel>) with Count, Sum and LINQ usage supported.
//    - CategoryId, LocationId (nullable ints) and Status (string) for filtering.
// 3. Create LowStockReportItemViewModel with all properties the view references:
//    - MaterialId, StockId (ints)
//    - MaterialCode, MaterialName, CategoryName, LocationName, Unit (strings)
//    - CurrentQuantity, MinimumStock (ints)
//    - ShortageQuantity computed as Max(0, MinimumStock - CurrentQuantity)
// 4. Initialize lists to avoid null reference exceptions in the view.
// 5. Keep the types simple and compile-safe (add necessary using directives).
//
// This file provides the minimal, self-contained types to resolve CS0246 for LowStockReportViewModel.
// If your project already has a different namespace convention, move these classes into the project's namespace
// or update the view's @model line to use the fully-qualified name.

using System;
using System.Collections.Generic;
using System.Linq;

public class LowStockReportViewModel
{
    public string InstitutionName { get; set; } = string.Empty;

    // Collection used heavily in the Razor view (Count, Sum, OrderBy, Any)
    public List<LowStockReportItemViewModel> Items { get; set; } = new List<LowStockReportItemViewModel>();

    // Filter bindings from the view
    public int? CategoryId { get; set; }
    public int? LocationId { get; set; }
    public string? Status { get; set; }

    // Helper properties commonly useful in views (optional)
    public int TotalItems => Items?.Count ?? 0;
    public int TotalShortage => Items?.Sum(i => i.ShortageQuantity) ?? 0;
}

public class LowStockReportItemViewModel
{
    // Identifiers
    public int MaterialId { get; set; }
    public int StockId { get; set; }

    // Descriptive fields used in the view
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;

    // Quantities
    public int CurrentQuantity { get; set; }
    public int MinimumStock { get; set; }

    // Computed shortage (used in the view)
    public int ShortageQuantity => Math.Max(0, MinimumStock - CurrentQuantity);
}