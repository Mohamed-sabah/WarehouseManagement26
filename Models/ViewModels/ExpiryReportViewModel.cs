/* 
PSEUDOCODE / PLAN (detailed):
- Provide a concrete view-model type named `ExpiryReportViewModel` so the Razor view's
  `@model ExpiryReportViewModel` reference resolves (fixes CS0246).
- The view expects a collection of items and several properties:
    - `Items` collection with elements exposing properties used in the view:
        StockId, MaterialCode, MaterialName, BatchNumber, CategoryName,
        LocationName, Quantity, Unit, ExpiryDate, DaysUntilExpiry, AtRiskValue
    - `InstitutionName` (string)
    - `DaysFilter` (int)
    - `CategoryId` (nullable int)
    - `LocationId` (nullable int)
- Create two public classes:
    - `ExpiryReportViewModel` with the expected container properties.
    - `ExpiryReportItemViewModel` for each row/item with the required fields and sensible types.
- Keep the file simple (no namespace) so Razor view using `@model ExpiryReportViewModel` will find the type
  without further changes. If you prefer a namespace in your project, move these classes into your project's
  namespace and update the view to `@model Your.Namespace.ExpiryReportViewModel` or add an `@using`.
- Ensure default initializers to avoid null-reference issues when the view enumerates `Items`.
*/

using System;
using System.Collections.Generic;

public class ExpiryReportViewModel
{
    // Institution title shown in print header
    public string InstitutionName { get; set; } = string.Empty;

    // Current filter in days (matches the select in the view)
    public int DaysFilter { get; set; }

    // Optional selected category / location ids (nullable to allow "all")
    public int? CategoryId { get; set; }
    public int? LocationId { get; set; }

    // Collection of items to render in the table
    public List<ExpiryReportItemViewModel> Items { get; set; } = new List<ExpiryReportItemViewModel>();
}

public class ExpiryReportItemViewModel
{
    public int StockId { get; set; }

    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string BatchNumber { get; set; } = string.Empty;

    public string CategoryName { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;

    // Quantity and unit displayed in the view (e.g. "10 kg")
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;

    // Expiry date and helper values
    public DateTime ExpiryDate { get; set; }
    public int DaysUntilExpiry { get; set; } // negative = already expired

    // Monetary value shown in the report footer and per-row
    public decimal AtRiskValue { get; set; }
}