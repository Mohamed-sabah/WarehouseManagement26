using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WarehouseManagement.Models.ViewModels
{
    #region Inventory Record ViewModels (Form 2)

    /// <summary>
    /// ViewModel لعرض قائمة الجرد (نموذج 2)
    /// </summary>
    public class InventoryListViewModel
    {
        [Display(Name = "السنة")]
        public int Year { get; set; } = DateTime.Now.Year;

        [Display(Name = "القسم/الدائرة")]
        public string? Department { get; set; }

        [Display(Name = "الموقع")]
        public int? LocationId { get; set; }

        [Display(Name = "البحث")]
        public string? SearchTerm { get; set; }

        [Display(Name = "الحالة")]
        public string? ConditionFilter { get; set; }

        [Display(Name = "عرض الفروقات فقط")]
        public bool ShowDifferencesOnly { get; set; } = false;

        // النتائج
        public List<InventoryRecord> Records { get; set; } = new();

        // الإحصائيات
        public InventoryStatistics Statistics { get; set; } = new();

        // قوائم الاختيار
        public List<int> AvailableYears { get; set; } = new();
        public List<string> Departments { get; set; } = new();
        public List<Location> Locations { get; set; } = new();
        public List<string> Conditions { get; set; } = new()
        {
            "ممتازة", "جيدة", "جيد", "متوسطة", "ضعيفة", "تالفة"
        };
    }

    /// <summary>
    /// إحصائيات الجرد
    /// </summary>
    public class InventoryStatistics
    {
        public int TotalItems { get; set; }
        public int TotalQuantityByInventory { get; set; }
        public int TotalQuantityByRecords { get; set; }
        public int TotalDifference { get; set; }
        public decimal TotalCost { get; set; }
        public int ItemsWithShortage { get; set; }
        public int ItemsWithSurplus { get; set; }
        public int ItemsMatching { get; set; }
        public int ItemsWithConsumption { get; set; }
        public DateTime GeneratedDate { get; set; } = DateTime.Now;

        // نسب
        public double ShortagePercentage => TotalItems > 0 ? (double)ItemsWithShortage / TotalItems * 100 : 0;
        public double SurplusPercentage => TotalItems > 0 ? (double)ItemsWithSurplus / TotalItems * 100 : 0;
        public double MatchingPercentage => TotalItems > 0 ? (double)ItemsMatching / TotalItems * 100 : 0;
    }

    /// <summary>
    /// ViewModel لإنشاء/تعديل سجل جرد
    /// </summary>
    public class InventoryCreateEditViewModel
    {
        public InventoryRecord Record { get; set; } = new();

        /// <summary>
        /// اختيار من المخزون مباشرة
        /// </summary>
        [Display(Name = "اختيار من المخزون")]
        public int? MaterialStockId { get; set; }

        /// <summary>
        /// تحديث المخزون تلقائياً بناءً على الفرق
        /// </summary>
        [Display(Name = "تحديث المخزون تلقائياً")]
        public bool UpdateStockAutomatically { get; set; } = false;

        // قوائم الاختيار
        public List<Material> Materials { get; set; } = new();
        public List<Location> Locations { get; set; } = new();
        public List<MaterialStock> AvailableStocks { get; set; } = new();
    }

    /// <summary>
    /// ViewModel لعملية الجرد الجماعي
    /// </summary>
    public class BulkInventoryViewModel
    {
        [Required]
        [Display(Name = "السنة")]
        public int Year { get; set; } = DateTime.Now.Year;

        [Required]
        [Display(Name = "تاريخ الجرد")]
        public DateTime InventoryDate { get; set; } = DateTime.Now;

        [Required]
        [Display(Name = "المسؤول عن الجرد")]
        public string InventoryBy { get; set; } = string.Empty;

        [Display(Name = "القسم/الدائرة")]
        public string? Department { get; set; }

        [Display(Name = "الموقع")]
        public int? LocationId { get; set; }

        /// <summary>
        /// عناصر الجرد
        /// </summary>
        public List<BulkInventoryItemViewModel> Items { get; set; } = new();

        // قوائم الاختيار
        public List<Location> Locations { get; set; } = new();
    }

    /// <summary>
    /// ViewModel لعنصر جرد فردي
    /// </summary>
    public class BulkInventoryItemViewModel
    {
        public int MaterialStockId { get; set; }
        public int MaterialId { get; set; }
        public string MaterialName { get; set; } = string.Empty;
        public string MaterialCode { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public string? CategoryName { get; set; }

        /// <summary>
        /// الكمية المسجلة في النظام
        /// </summary>
        public int RecordedQuantity { get; set; }

        /// <summary>
        /// الكمية الفعلية (يدخلها المستخدم)
        /// </summary>
        [Display(Name = "الكمية الفعلية")]
        public int ActualQuantity { get; set; }

        /// <summary>
        /// الفرق
        /// </summary>
        public int Difference => ActualQuantity - RecordedQuantity;

        [Display(Name = "الحالة")]
        public MaterialCondition Condition { get; set; } = MaterialCondition.Good;

        [Display(Name = "ملاحظات")]
        public string? Notes { get; set; }

        [Display(Name = "اختيار للجرد")]
        public bool IsSelected { get; set; } = true;
    }

    /// <summary>
    /// ViewModel لتقرير الجرد (للطباعة)
    /// </summary>
    public class InventoryReportViewModel
    {
        [Display(Name = "السنة")]
        public int Year { get; set; }

        [Display(Name = "القسم/الدائرة")]
        public string Department { get; set; } = string.Empty;

        [Display(Name = "اسم الموقع")]
        public string? LocationName { get; set; }

        [Display(Name = "تاريخ التقرير")]
        public DateTime ReportDate { get; set; } = DateTime.Now;

        [Display(Name = "عناصر الجرد")]
        public List<InventoryRecord> Records { get; set; } = new();

        [Display(Name = "الإحصائيات")]
        public InventoryStatistics Statistics { get; set; } = new();

        // معلومات للطباعة
        public string ReportTitle { get; set; } = "نموذج رقم (2) - قائمة الموجودات الرئيسية";
        public string OrganizationName { get; set; } = string.Empty;
        public string PreparedBy { get; set; } = string.Empty;
        public string ApprovedBy { get; set; } = string.Empty;
    }

    /// <summary>
    /// ViewModel لتحويل عنصر جرد إلى استهلاك (نموذج 5)
    /// </summary>
    public class TransferToConsumptionViewModel
    {
        [Required]
        public int InventoryRecordId { get; set; }

        public InventoryRecord? InventoryRecord { get; set; }

        [Required(ErrorMessage = "الكمية المستهلكة مطلوبة")]
        [Range(1, int.MaxValue, ErrorMessage = "الكمية يجب أن تكون أكبر من صفر")]
        [Display(Name = "الكمية المستهلكة")]
        public int ConsumedQuantity { get; set; }

        /// <summary>
        /// الكمية (بديل لـ ConsumedQuantity)
        /// </summary>
        [Display(Name = "الكمية")]
        public int Quantity { get => ConsumedQuantity; set => ConsumedQuantity = value; }

        [Required]
        [Display(Name = "سبب الاستهلاك")]
        public ConsumptionReason Reason { get; set; } = ConsumptionReason.NormalUse;

        [Display(Name = "تفاصيل السبب")]
        public string? ReasonDetails { get; set; }

        /// <summary>
        /// وصف السبب (بديل)
        /// </summary>
        [Display(Name = "وصف السبب")]
        public string? ReasonDescription { get => ReasonDetails; set => ReasonDetails = value; }

        [Required]
        [Display(Name = "قرار اللجنة")]
        public CommitteeDecision Decision { get; set; } = CommitteeDecision.UnderReview;

        [Display(Name = "تفاصيل القرار")]
        public string? DecisionDetails { get; set; }

        /// <summary>
        /// ملاحظات القرار (بديل)
        /// </summary>
        [Display(Name = "ملاحظات القرار")]
        public string? DecisionNotes { get => DecisionDetails; set => DecisionDetails = value; }

        [Display(Name = "نسبة الضرر (%)")]
        [Range(0, 100)]
        public decimal? DamagePercentage { get; set; }

        [Display(Name = "القيمة المتبقية")]
        public decimal? ResidualValue { get; set; }

        [Display(Name = "ملاحظات")]
        public string? Notes { get; set; }

        /// <summary>
        /// خصم من المخزون تلقائياً
        /// </summary>
        [Display(Name = "خصم من المخزون تلقائياً")]
        public bool DeductFromStock { get; set; } = true;
    }

    #endregion
}