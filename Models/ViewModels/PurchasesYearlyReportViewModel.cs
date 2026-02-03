// PSEUDOCODE / PLAN (detailed):
// 1. Create a view-model type named `PurchasesYearlyReportViewModel` so the Razor view's
//    `@model PurchasesYearlyReportViewModel` resolves and CS0246 is fixed.
// 2. Include properties observed in the view:
//    - Year : int
//    - InstitutionName : string
//    - TotalPurchases : int
//    - TotalQuantity : decimal
//    - TotalValue : decimal
//    - MonthlyData : collection of monthly summary items (Month, PurchasesCount, Quantity, Value)
//    - CategoryData : collection of category summary items (CategoryName, PurchasesCount, Quantity, Value)
//    - TopSuppliers : collection of supplier summary items (SupplierName, PurchasesCount, Value)
// 3. Create lightweight DTO classes for monthly, category and supplier rows with the fields
//    required by the view. Use types compatible with view formatting methods (ToString("N0"), arithmetic).
// 4. Provide default initializers for collections to avoid null checks in the view.
// 5. Place the types in a file that will be compiled with the project so Razor can find the type.
//    (No namespace is used to make the simple type name `PurchasesYearlyReportViewModel` resolve
//     directly from the view without adding `@using` — adjust namespace as needed for your project).
//
// Note: If your project uses a specific root namespace or folder convention (e.g., `MyApp.ViewModels`),
// move these classes into that namespace or add `@using` in your Razor view accordingly.

using System;
using System.Collections.Generic;

public class PurchasesYearlyReportViewModel
{
    // The year for the report (e.g., 2025)
    public int Year { get; set; }

    // Name of the institution shown in the print header
    public string InstitutionName { get; set; } = string.Empty;

    // Aggregate statistics used by the top cards and footer
    public int TotalPurchases { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal TotalValue { get; set; }

    // Monthly data (expected 12 entries for a full year but flexible)
    public List<MonthlyDataItem> MonthlyData { get; set; } = new List<MonthlyDataItem>();

    // Summary by category
    public List<CategorySummaryItem> CategoryData { get; set; } = new List<CategorySummaryItem>();

    // Top suppliers (may be null or empty)
    public List<SupplierSummaryItem>? TopSuppliers { get; set; } = new List<SupplierSummaryItem>();
}

public class MonthlyDataItem
{
    // Month number: 1 = January, ... 12 = December
    public int Month { get; set; }

    // Number of purchase operations in the month
    public int PurchasesCount { get; set; }

    // Quantity purchased (use decimal to allow fractional quantities if needed)
    public decimal Quantity { get; set; }

    // Total value for the month (same unit as TotalValue)
    public decimal Value { get; set; }
}

public class CategorySummaryItem
{
    public string CategoryName { get; set; } = string.Empty;
    public int PurchasesCount { get; set; }
    public decimal Quantity { get; set; }
    public decimal Value { get; set; }
}

public class SupplierSummaryItem
{
    public string SupplierName { get; set; } = string.Empty;
    public int PurchasesCount { get; set; }
    public decimal Value { get; set; }
}