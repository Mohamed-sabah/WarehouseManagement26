// PSEUDOCODE / PLAN (detailed):
// 1. Define a view model class named `ConsumptionForm5ViewModel` in a namespace that your project can import.
//    - Properties needed by the Razor view:
//      - DateTime StartDate
//      - DateTime EndDate
//      - int? DepartmentId
//      - string MinistryName
//      - string InstitutionName
//      - string WarehouseName
//      - string WarehouseManager
//      - string Notes
//      - List<ConsumptionForm5ItemViewModel> Items
// 2. Define a supporting item view model `ConsumptionForm5ItemViewModel` with properties used in the view:
//      - DateTime ConsumptionDate
//      - string VoucherNumber
//      - string MaterialCode
//      - string MaterialName
//      - string Unit
//      - decimal Quantity
//      - decimal UnitPrice
//      - decimal TotalValue (computed)
//      - string DepartmentName
//      - string ReceiverName
// 3. Provide default initializers for collections to avoid null checks in the view.
// 4. Add this file under `ViewModels/Reports` so it is discoverable; either update `_ViewImports.cshtml` or the view to import the namespace
//    OR change the view's `@model` line to the fully qualified name:
//      @model YourProject.ViewModels.Reports.ConsumptionForm5ViewModel
// 5. This will resolve CS0246 (type not found) by ensuring the model type exists and is in an importable namespace.
//
// IMPLEMENTATION:
using System;
using System.Collections.Generic;

namespace YourProject.ViewModels.Reports
{
    /// <summary>
    /// ViewModel for the "نموذج رقم (5)" consumption report view.
    /// Matches the properties referenced by Views/Reports/ConsumptionForm5.cshtml.
    /// </summary>
    public class ConsumptionForm5ViewModel
    {
        public DateTime StartDate { get; set; } = DateTime.Today;
        public DateTime EndDate { get; set; } = DateTime.Today;
        public int? DepartmentId { get; set; }

        public string MinistryName { get; set; } = string.Empty;
        public string InstitutionName { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public string WarehouseManager { get; set; } = string.Empty;

        public string? Notes { get; set; }

        // Collection of report items
        public List<ConsumptionForm5ItemViewModel> Items { get; set; } = new List<ConsumptionForm5ItemViewModel>();
    }

    /// <summary>
    /// Represents a single consumption/issuance record displayed in the report.
    /// </summary>
    public class ConsumptionForm5ItemViewModel
    {
        public DateTime ConsumptionDate { get; set; }

        public string VoucherNumber { get; set; } = string.Empty;
        public string MaterialCode { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;

        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        // Computed property used in the view
        public decimal TotalValue => Quantity * UnitPrice;

        public string DepartmentName { get; set; } = string.Empty;
        public string ReceiverName { get; set; } = string.Empty;
    }
}