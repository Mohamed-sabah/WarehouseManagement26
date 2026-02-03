/* 
Pseudocode / Plan (detailed):
1. Provide a concrete public class named `MaterialsValueReportViewModel` so the Razor view's
   `@model MaterialsValueReportViewModel` can resolve to a type.
2. The view references the following members on the model:
   - Items: enumerable of items with properties: MaterialCode, MaterialName, CategoryName, Unit,
            Quantity, UnitPrice, TotalValue
   - TotalQuantity: numeric (used with ToString("N0"))
   - TotalValue: numeric (used with ToString("N0"))
   - SortBy: string
   - CategoryId, LocationId: nullable ints (used to set selected on <select>)
   - InstitutionName: string (used in print header)
3. Implement a small item DTO `MaterialsValueItem` with appropriate types:
   - string MaterialCode, MaterialName, CategoryName, Unit
   - decimal Quantity, UnitPrice
   - decimal TotalValue (computed as Quantity * UnitPrice but allow set if caller provides)
4. Implement `MaterialsValueReportViewModel` with:
   - a List<MaterialsValueItem> Items (initialized)
   - TotalQuantity and TotalValue implemented as computed properties (sum of Items) to avoid
     stale values and ensure the view shows consistent totals
   - SortBy, CategoryId, LocationId, InstitutionName properties
5. Keep the class in the global namespace (no namespace declaration) to avoid requiring changes
   to the Razor view's `@model` line. This avoids guessing your project's root namespace.
6. Keep the implementation minimal, typed, and safe for use in the view.

Note: If your project uses a root namespace or a specific `ViewModels` namespace and you prefer
the class to live there, move this file into that namespace and update the Razor `@model`
to the fully qualified name (for example: `@model MyApp.ViewModels.MaterialsValueReportViewModel`).
*/

using System;
using System.Collections.Generic;
using System.Linq;

public class MaterialsValueReportViewModel
{
    // Items used by the view's foreach loop
    public List<MaterialsValueItem> Items { get; set; } = new List<MaterialsValueItem>();

    // Computed total quantity (sums Item.Quantity)
    public decimal TotalQuantity
    {
        get => Items?.Sum(i => i.Quantity) ?? 0m;
    }

    // Computed total value (sums Item.TotalValue)
    public decimal TotalValue
    {
        get => Items?.Sum(i => i.TotalValue) ?? 0m;
    }

    // Filtering / UI state
    public string SortBy { get; set; } = "value";
    public int? CategoryId { get; set; }
    public int? LocationId { get; set; }

    // Printed header
    public string InstitutionName { get; set; } = string.Empty;
}

public class MaterialsValueItem
{
    // Basic identifiers / display fields
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;

    // Quantitative fields
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    // Allow explicit TotalValue but compute fallback if not set
    private decimal? _totalValue;
    public decimal TotalValue
    {
        get
        {
            if (_totalValue.HasValue)
                return _totalValue.Value;

            // Compute as Quantity * UnitPrice by default
            return Quantity * UnitPrice;
        }
        set => _totalValue = value;
    }
}