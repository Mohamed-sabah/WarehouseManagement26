using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WarehouseManagement.Models.ViewModels
{
    #region Consumption Record ViewModels (Form 5)

    /// <summary>
    /// ViewModel لعرض قائمة الاستهلاك (نموذج 5)
    /// </summary>
    public class ConsumptionListViewModel
    {
        [Display(Name = "القسم/الدائرة")]
        public string? Department { get; set; }

        [Display(Name = "البحث")]
        public string? SearchTerm { get; set; }

        [Display(Name = "سبب الاستهلاك")]
        public ConsumptionReason? ReasonFilter { get; set; }

        [Display(Name = "قرار اللجنة")]
        public CommitteeDecision? DecisionFilter { get; set; }

        [Display(Name = "السنة")]
        public int? YearFilter { get; set; }

        [Display(Name = "تم التصرف")]
        public bool? IsDisposedFilter { get; set; }

        // النتائج
        public List<ConsumptionRecord> Records { get; set; } = new();

        // الإحصائيات
        public ConsumptionStatistics Statistics { get; set; } = new();

        // قوائم الاختيار
        public List<string> Departments { get; set; } = new();
        public List<int> AvailableYears { get; set; } = new();
    }

    /// <summary>
    /// إحصائيات الاستهلاك
    /// </summary>
    public class ConsumptionStatistics
    {
        public int TotalItems { get; set; }
        public int TotalQuantity { get; set; }
        public decimal TotalOriginalValue { get; set; }
        public decimal TotalResidualValue { get; set; }
        public decimal TotalLoss => TotalOriginalValue - TotalResidualValue;
        public int ItemsDisposed { get; set; }
        public int ItemsPending { get; set; }

        // تجميع حسب السبب
        public Dictionary<ConsumptionReason, int> ByReason { get; set; } = new();

        // تجميع حسب القرار
        public Dictionary<CommitteeDecision, int> ByDecision { get; set; } = new();

        public DateTime GeneratedDate { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// ViewModel لإنشاء/تعديل سجل استهلاك
    /// </summary>
    public class ConsumptionCreateEditViewModel
    {
        public ConsumptionRecord Record { get; set; } = new();

        /// <summary>
        /// اختيار من سجلات الجرد
        /// </summary>
        [Display(Name = "سجل الجرد")]
        public int? SelectedInventoryRecordId { get; set; }

        /// <summary>
        /// خصم من المخزون تلقائياً
        /// </summary>
        [Display(Name = "خصم من المخزون تلقائياً")]
        public bool DeductFromStockAutomatically { get; set; } = true;

        // قوائم الاختيار
        public List<InventoryRecordSelectViewModel> AvailableInventoryRecords { get; set; } = new();
    }

    /// <summary>
    /// ViewModel لاختيار سجل جرد
    /// </summary>
    public class InventoryRecordSelectViewModel
    {
        public int Id { get; set; }
        public string MaterialName { get; set; } = string.Empty;
        public string MaterialCode { get; set; } = string.Empty;
        public int AvailableQuantity { get; set; }
        public decimal TotalCost { get; set; }
        public string? Department { get; set; }
        public int Year { get; set; }
    }

    /// <summary>
    /// ViewModel لتقرير الاستهلاك (للطباعة)
    /// </summary>
    public class ConsumptionReportViewModel
    {
        [Display(Name = "تاريخ التقرير")]
        public DateTime ReportDate { get; set; }

        [Display(Name = "القسم/الدائرة")]
        public string Department { get; set; } = string.Empty;

        [Display(Name = "عناصر الاستهلاك")]
        public List<ConsumptionRecord> Records { get; set; } = new();

        [Display(Name = "الإحصائيات")]
        public ConsumptionStatistics Statistics { get; set; } = new();

        // معلومات للطباعة
        public string ReportTitle { get; set; } = "نموذج رقم (5) - قائمة بالموجودات المستهلكة";
        public string OrganizationName { get; set; } = string.Empty;
        public string PreparedBy { get; set; } = string.Empty;
        public string CommitteeMembers { get; set; } = string.Empty;
    }

    /// <summary>
    /// ViewModel لتنفيذ قرار اللجنة
    /// </summary>
    public class ProcessDecisionViewModel
    {
        [Required]
        public int ConsumptionRecordId { get; set; }

        public ConsumptionRecord? Record { get; set; }

        [Required]
        [Display(Name = "الإجراء")]
        public DisposalAction Action { get; set; }

        [Display(Name = "رقم محضر التصرف")]
        public string? DisposalRecordNumber { get; set; }

        [Display(Name = "طريقة التصرف")]
        public string? DisposalMethod { get; set; }

        [Display(Name = "قيمة البيع")]
        public decimal? SaleValue { get; set; }

        [Display(Name = "ملاحظات")]
        public string? Notes { get; set; }
    }

    public enum DisposalAction
    {
        [Display(Name = "تنفيذ التصرف")]
        Dispose = 1,

        [Display(Name = "إلغاء التصرف (إرجاع)")]
        Revert = 2,

        [Display(Name = "تحديث القرار")]
        UpdateDecision = 3
    }

    /// <summary>
    /// ViewModel لتقرير المواد الأكثر استهلاكاً
    /// </summary>
    public class TopConsumedMaterialsViewModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<MaterialConsumptionSummary> Materials { get; set; } = new();
    }

    /// <summary>
    /// ملخص استهلاك مادة
    /// </summary>
    public class MaterialConsumptionSummary
    {
        public int MaterialId { get; set; }
        public string MaterialName { get; set; } = string.Empty;
        public string MaterialCode { get; set; } = string.Empty;
        public string? CategoryName { get; set; }
        public int TotalConsumedQuantity { get; set; }
        public decimal TotalLossValue { get; set; }
        public double AverageDamagePercentage { get; set; }
        public int ConsumptionCount { get; set; }
        public ConsumptionReason MostCommonReason { get; set; }
    }

    #endregion
}
