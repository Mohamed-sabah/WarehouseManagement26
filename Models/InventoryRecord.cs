using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace WarehouseManagement.Models
{
    /// <summary>
    /// سجل الجرد السنوي - يمثل نموذج رقم (2)
    /// InventoryRecord - Annual Inventory Record (Form 2)
    /// قائمة الموجودات الرئيسية
    /// </summary>
    public class InventoryRecord
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// رقم التسلسل في الجرد
        /// </summary>
        [Display(Name = "ت")]
        public int SequenceNumber { get; set; }

        /// <summary>
        /// المادة - مرتبطة بجدول Materials
        /// </summary>
        [Required]
        [Display(Name = "المادة")]
        public int MaterialId { get; set; }

        /// <summary>
        /// الموقع / المخزن
        /// </summary>
        [Required]
        [Display(Name = "الموقع")]
        public int LocationId { get; set; }

        /// <summary>
        /// سنة الجرد
        /// </summary>
        [Required]
        [Display(Name = "سنة الجرد")]
        public int Year { get; set; } = DateTime.Now.Year;

        /// <summary>
        /// تاريخ الجرد
        /// </summary>
        [Required]
        [Display(Name = "تاريخ الجرد")]
        [DataType(DataType.Date)]
        public DateTime InventoryDate { get; set; } = DateTime.Now;

        /// <summary>
        /// الكمية الفعلية بموجب الجرد
        /// </summary>
        [Required]
        [Display(Name = "بموجب الجرد")]
        public int ActualQuantity { get; set; }

        /// <summary>
        /// الكمية بموجب السجلات
        /// </summary>
        [Required]
        [Display(Name = "بموجب السجلات")]
        public int RecordedQuantity { get; set; }

        /// <summary>
        /// الفرق (محسوب)
        /// </summary>
        [NotMapped]
        [Display(Name = "الفرق")]
        public int Difference => ActualQuantity - RecordedQuantity;

        /// <summary>
        /// الفرق المخزن (للبحث والفلترة)
        /// </summary>
        [Display(Name = "الفرق")]
        public int StoredDifference { get; set; }

        /// <summary>
        /// حالة المادة (ممتازة/جيدة/جيد/متوسطة/ضعيفة/تالفة)
        /// </summary>
        [StringLength(50)]
        [Display(Name = "الحالة")]
        public string Condition { get; set; } = "جيدة";

        /// <summary>
        /// محل التواجد الفعلي
        /// </summary>
        [StringLength(200)]
        [Display(Name = "محل التواجد")]
        public string? ActualLocation { get; set; }

        /// <summary>
        /// القسم / الدائرة / العائدية
        /// </summary>
        [StringLength(200)]
        [Display(Name = "القسم/العائدية")]
        public string? Department { get; set; }

        /// <summary>
        /// المسؤول عن الجرد
        /// </summary>
        [StringLength(100)]
        [Display(Name = "المسؤول عن الجرد")]
        public string? InventoryBy { get; set; }

        /// <summary>
        /// هل تم التصديق على الجرد
        /// </summary>
        [Display(Name = "تم التصديق")]
        public bool IsApproved { get; set; } = false;

        /// <summary>
        /// تاريخ التصديق
        /// </summary>
        [Display(Name = "تاريخ التصديق")]
        public DateTime? ApprovedDate { get; set; }

        /// <summary>
        /// المصادق
        /// </summary>
        [StringLength(100)]
        [Display(Name = "تم التصديق بواسطة")]
        public string? ApprovedBy { get; set; }

        /// <summary>
        /// هل تم تحديث المخزون بناءً على الجرد
        /// </summary>
        [Display(Name = "تم تحديث المخزون")]
        public bool IsStockUpdated { get; set; } = false;

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

        public virtual Material Material { get; set; } = null!;
        public virtual Location Location { get; set; } = null!;

        /// <summary>
        /// سجلات الاستهلاك المرتبطة (نموذج 5)
        /// </summary>
        public virtual ICollection<ConsumptionRecord> Consumptions { get; set; } = new List<ConsumptionRecord>();

        // ============ Computed Properties ============

        /// <summary>
        /// الكلفة الإجمالية (من سجلات الشراء)
        /// </summary>
        [NotMapped]
        [Display(Name = "الكلفة بالدينار")]
        public decimal TotalCost
        {
            get
            {
                if (Material?.Purchases == null || !Material.Purchases.Any())
                    return 0;

                // حساب متوسط السعر المرجح
                var relevantPurchases = Material.Purchases
                    .Where(p => p.PurchaseDate <= InventoryDate)
                    .ToList();

                if (!relevantPurchases.Any())
                    return ActualQuantity * (Material.LastPurchasePrice ?? 0);

                var avgPrice = relevantPurchases.Sum(p => p.TotalPrice) /
                              relevantPurchases.Sum(p => p.Quantity);

                return ActualQuantity * avgPrice;
            }
        }

        /// <summary>
        /// هل يوجد نقص في المخزون
        /// </summary>
        [NotMapped]
        public bool HasShortage => Difference < 0;

        /// <summary>
        /// هل يوجد فائض في المخزون
        /// </summary>
        [NotMapped]
        public bool HasSurplus => Difference > 0;

        /// <summary>
        /// هل الكميات متطابقة
        /// </summary>
        [NotMapped]
        public bool IsMatching => Difference == 0;

        /// <summary>
        /// هل تم تسجيل استهلاك (نموذج 5)
        /// </summary>
        [NotMapped]
        public bool HasConsumptions => Consumptions?.Any() ?? false;

        /// <summary>
        /// إجمالي الكمية المستهلكة
        /// </summary>
        [NotMapped]
        public int TotalConsumedQuantity => Consumptions?.Sum(c => c.ConsumedQuantity) ?? 0;

        /// <summary>
        /// سعر الوحدة (من سجلات الشراء)
        /// </summary>
        [NotMapped]
        [Display(Name = "سعر الوحدة")]
        public decimal UnitPrice => Material?.AveragePrice ?? 0;
    }

    /// <summary>
    /// حالة المادة في الجرد
    /// </summary>
    public enum MaterialCondition
    {
        [Display(Name = "ممتازة")]
        Excellent = 1,

        [Display(Name = "جيدة")]
        Good = 2,

        [Display(Name = "جيد")]
        GoodMale = 3,

        [Display(Name = "متوسطة")]
        Average = 4,

        [Display(Name = "ضعيفة")]
        Poor = 5,

        [Display(Name = "تالفة")]
        Damaged = 6
    }
}