using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WarehouseManagement.Models
{
    /// <summary>
    /// سجل الاستهلاك - يمثل نموذج رقم (5)
    /// ConsumptionRecord - Consumption Record (Form 5)
    /// قائمة بالموجودات المستهلكة
    /// </summary>
    public class ConsumptionRecord
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// رقم التسلسل
        /// </summary>
        [Display(Name = "ت")]
        public int SequenceNumber { get; set; }

        /// <summary>
        /// سجل الجرد المرتبط (نموذج 2)
        /// هذا هو الربط الأساسي - الاستهلاك مرتبط بسجل جرد
        /// </summary>
        [Required]
        [Display(Name = "سجل الجرد")]
        public int InventoryRecordId { get; set; }

        /// <summary>
        /// الكمية المستهلكة
        /// </summary>
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "الكمية يجب أن تكون أكبر من صفر")]
        [Display(Name = "الكمية المستهلكة")]
        public int ConsumedQuantity { get; set; }

        /// <summary>
        /// تاريخ الاستهلاك / تاريخ التقرير
        /// </summary>
        [Required]
        [Display(Name = "تاريخ التقرير")]
        [DataType(DataType.Date)]
        public DateTime ReportDate { get; set; } = DateTime.Now;

        /// <summary>
        /// نسبة الضرر (%)
        /// </summary>
        [Display(Name = "نسبة الضرر (%)")]
        [Range(0, 100)]
        [Column(TypeName = "decimal(5,2)")]
        public decimal? DamagePercentage { get; set; }

        /// <summary>
        /// مدة الاستعمال (نص)
        /// </summary>
        [StringLength(100)]
        [Display(Name = "مدة الاستعمال")]
        public string? UsageDuration { get; set; }

        /// <summary>
        /// مدة الاستعمال بالأيام (محسوب)
        /// </summary>
        [Display(Name = "مدة الاستعمال (أيام)")]
        public int? UsageDurationDays { get; set; }

        /// <summary>
        /// سبب الاستهلاك
        /// </summary>
        [Required]
        [Display(Name = "سبب الاستهلاك")]
        public ConsumptionReason Reason { get; set; } = ConsumptionReason.NormalUse;

        /// <summary>
        /// تفاصيل سبب الاستهلاك
        /// </summary>
        [StringLength(500)]
        [Display(Name = "تفاصيل السبب")]
        public string? ReasonDetails { get; set; }

        /// <summary>
        /// وصف السبب (بديل لـ ReasonDetails)
        /// </summary>
        [StringLength(500)]
        [Display(Name = "وصف السبب")]
        public string? ReasonDescription { get; set; }

        /// <summary>
        /// قرار / توجيهات اللجنة
        /// </summary>
        [Required]
        [Display(Name = "قرار اللجنة")]
        public CommitteeDecision Decision { get; set; } = CommitteeDecision.UnderReview;

        /// <summary>
        /// تفاصيل قرار اللجنة
        /// </summary>
        [StringLength(500)]
        [Display(Name = "تفاصيل القرار")]
        public string? DecisionDetails { get; set; }

        /// <summary>
        /// ملاحظات القرار (بديل)
        /// </summary>
        [StringLength(500)]
        [Display(Name = "ملاحظات القرار")]
        public string? DecisionNotes { get; set; }

        /// <summary>
        /// توصيات اللجنة
        /// </summary>
        [StringLength(500)]
        [Display(Name = "توصيات اللجنة")]
        public string? CommitteeRecommendations { get; set; }

        /// <summary>
        /// أعضاء اللجنة
        /// </summary>
        [StringLength(500)]
        [Display(Name = "أعضاء اللجنة")]
        public string? CommitteeMembers { get; set; }

        /// <summary>
        /// السعر الأصلي للمفرد (من سجل الشراء)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "سعر المفرد الأصلي")]
        public decimal OriginalUnitPrice { get; set; }

        /// <summary>
        /// القيمة الأصلية الإجمالية
        /// </summary>
        [NotMapped]
        [Display(Name = "القيمة الأصلية")]
        public decimal OriginalTotalValue => ConsumedQuantity * OriginalUnitPrice;

        /// <summary>
        /// القيمة الأصلية (بديل)
        /// </summary>
        [NotMapped]
        [Display(Name = "القيمة الأصلية")]
        public decimal OriginalValue => StoredOriginalValue > 0 ? StoredOriginalValue : OriginalTotalValue;

        /// <summary>
        /// القيمة الأصلية المخزنة
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "القيمة الأصلية")]
        public decimal StoredOriginalValue { get; set; }

        /// <summary>
        /// القيمة المتبقية (محسوبة)
        /// </summary>
        [NotMapped]
        [Display(Name = "القيمة المتبقية")]
        public decimal ResidualValue => DamagePercentage.HasValue
            ? OriginalTotalValue * (100 - DamagePercentage.Value) / 100
            : OriginalTotalValue;

        /// <summary>
        /// القيمة المتبقية المخزنة
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "القيمة المتبقية")]
        public decimal StoredResidualValue { get; set; }

        /// <summary>
        /// قيمة الخسارة
        /// </summary>
        [NotMapped]
        [Display(Name = "قيمة الخسارة")]
        public decimal LossValue => OriginalTotalValue - ResidualValue;

        /// <summary>
        /// هل تم التصرف بالمادة
        /// </summary>
        [Display(Name = "تم التصرف")]
        public bool IsDisposed { get; set; } = false;

        /// <summary>
        /// تاريخ التصرف
        /// </summary>
        [Display(Name = "تاريخ التصرف")]
        public DateTime? DisposalDate { get; set; }

        /// <summary>
        /// رقم محضر التصرف
        /// </summary>
        [StringLength(50)]
        [Display(Name = "رقم محضر التصرف")]
        public string? DisposalRecordNumber { get; set; }

        /// <summary>
        /// طريقة التصرف
        /// </summary>
        [StringLength(200)]
        [Display(Name = "طريقة التصرف")]
        public string? DisposalMethod { get; set; }

        /// <summary>
        /// قيمة البيع (إذا تم البيع)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "قيمة البيع")]
        public decimal? SaleValue { get; set; }

        /// <summary>
        /// هل تم خصم الكمية من المخزون
        /// </summary>
        [Display(Name = "تم خصم المخزون")]
        public bool IsStockDeducted { get; set; } = false;

        /// <summary>
        /// الموقع الحالي للمادة المستهلكة
        /// </summary>
        [StringLength(200)]
        [Display(Name = "الموقع الحالي")]
        public string? CurrentLocation { get; set; }

        /// <summary>
        /// ملاحظات
        /// </summary>
        [StringLength(1000)]
        [Display(Name = "ملاحظات")]
        public string? Notes { get; set; }

        [Display(Name = "تاريخ الإنشاء")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "آخر تحديث")]
        public DateTime LastUpdated { get; set; } = DateTime.Now;

        [StringLength(100)]
        [Display(Name = "أنشئ بواسطة")]
        public string? CreatedBy { get; set; }

        // ============ Navigation Properties ============

        /// <summary>
        /// سجل الجرد المرتبط
        /// </summary>
        public virtual InventoryRecord InventoryRecord { get; set; } = null!;

        // ============ Computed Properties من سجل الجرد ============

        /// <summary>
        /// اسم المادة (من سجل الجرد)
        /// </summary>
        [NotMapped]
        [Display(Name = "اسم المادة")]
        public string MaterialName => InventoryRecord?.Material?.Name ?? "";

        /// <summary>
        /// رمز المادة (من سجل الجرد)
        /// </summary>
        [NotMapped]
        [Display(Name = "رمز المادة")]
        public string MaterialCode => InventoryRecord?.Material?.Code ?? "";

        /// <summary>
        /// القسم / الدائرة (من سجل الجرد)
        /// </summary>
        [NotMapped]
        [Display(Name = "القسم")]
        public string? Department => InventoryRecord?.Department;

        /// <summary>
        /// سنة الشراء (من أول عملية شراء)
        /// </summary>
        [NotMapped]
        [Display(Name = "سنة الشراء")]
        public int? PurchaseYear => InventoryRecord?.Material?.Purchases?
            .OrderBy(p => p.PurchaseDate)
            .FirstOrDefault()?.PurchaseYear;
    }

    /// <summary>
    /// أسباب الاستهلاك
    /// </summary>
    public enum ConsumptionReason
    {
        [Display(Name = "الاستعمال الاعتيادي")]
        NormalUse = 1,

        [Display(Name = "جراء الاستعمال")]
        RegularUse = 2,

        [Display(Name = "القدم")]
        Obsolescence = 3,

        [Display(Name = "عطل فني")]
        TechnicalFailure = 4,

        [Display(Name = "تقصير المسؤولين")]
        NegligenceByOfficials = 5,

        [Display(Name = "حادث")]
        Accident = 6,

        [Display(Name = "سوء استخدام")]
        Misuse = 7,

        [Display(Name = "عوامل خارجية")]
        ExternalFactors = 8,

        [Display(Name = "انتهاء العمر الافتراضي")]
        EndOfLife = 9,

        [Display(Name = "انتهاء فترة الصلاحية")]
        EndOfUsefulLife = 10,

        [Display(Name = "أخرى")]
        Other = 99
    }

    /// <summary>
    /// قرار / توجيهات اللجنة
    /// </summary>
    public enum CommitteeDecision
    {
        [Display(Name = "قيد الدراسة")]
        UnderReview = 0,

        [Display(Name = "إعادة استعمال كلي")]
        FullReuse = 1,

        [Display(Name = "إعادة استعمال جزئي")]
        PartialReuse = 2,

        [Display(Name = "بيع")]
        Sell = 3,

        [Display(Name = "إتلاف")]
        Dispose = 4,

        [Display(Name = "إصلاح")]
        Repair = 5,

        [Display(Name = "تحويل للخردة")]
        Scrap = 6,

        [Display(Name = "التصرف / الإسقاط")]
        Disposal = 7
    }
}