using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WarehouseManagement.Models
{
    /// <summary>
    /// مخزون المادة في موقع معين
    /// MaterialStock - Stock of material in a specific location
    /// يمكن أن تكون نفس المادة في عدة مواقع بكميات مختلفة
    /// </summary>
    public class MaterialStock
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// المادة
        /// </summary>
        [Required]
        [Display(Name = "المادة")]
        public int MaterialId { get; set; }

        /// <summary>
        /// الموقع
        /// </summary>
        [Required]
        [Display(Name = "الموقع")]
        public int LocationId { get; set; }

        /// <summary>
        /// الكمية المتوفرة
        /// </summary>
        [Required]
        [Display(Name = "الكمية")]
        public int Quantity { get; set; }

        /// <summary>
        /// الكمية المحجوزة (للطلبات قيد التنفيذ)
        /// </summary>
        [Display(Name = "الكمية المحجوزة")]
        public int ReservedQuantity { get; set; } = 0;

        /// <summary>
        /// الكمية المتاحة للاستخدام
        /// </summary>
        [NotMapped]
        [Display(Name = "الكمية المتاحة")]
        public int AvailableQuantity => Quantity - ReservedQuantity;

        /// <summary>
        /// تاريخ انتهاء الصلاحية (إن وجد)
        /// </summary>
        [Display(Name = "تاريخ انتهاء الصلاحية")]
        public DateTime? ExpiryDate { get; set; }

        /// <summary>
        /// رقم الدفعة / الباتش
        /// </summary>
        [StringLength(50)]
        [Display(Name = "رقم الدفعة")]
        public string? BatchNumber { get; set; }

        /// <summary>
        /// حالة المادة في هذا الموقع
        /// </summary>
        [StringLength(50)]
        [Display(Name = "الحالة")]
        public string Condition { get; set; } = "جيدة";

        /// <summary>
        /// محل التواجد الدقيق (رف/صف/عمود)
        /// </summary>
        [StringLength(100)]
        [Display(Name = "محل التواجد الدقيق")]
        public string? ExactLocation { get; set; }

        /// <summary>
        /// ملاحظات
        /// </summary>
        [StringLength(500)]
        [Display(Name = "ملاحظات")]
        public string? Notes { get; set; }

        [Display(Name = "آخر تحديث")]
        public DateTime LastUpdated { get; set; } = DateTime.Now;

        [Display(Name = "تاريخ الإنشاء")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // ============ Navigation Properties ============

        public virtual Material Material { get; set; } = null!;
        public virtual Location Location { get; set; } = null!;

        // ============ Computed Properties ============

        /// <summary>
        /// هل انتهت الصلاحية
        /// </summary>
        [NotMapped]
        public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value < DateTime.Now;

        /// <summary>
        /// هل ستنتهي الصلاحية قريباً (خلال 30 يوم)
        /// </summary>
        [NotMapped]
        public bool IsExpiringSoon => ExpiryDate.HasValue && 
            ExpiryDate.Value >= DateTime.Now && 
            ExpiryDate.Value <= DateTime.Now.AddDays(30);

        /// <summary>
        /// القيمة الحالية للمخزون في هذا الموقع
        /// </summary>
        [NotMapped]
        [Display(Name = "القيمة")]
        public decimal CurrentValue => Quantity * (Material?.LastPurchasePrice ?? Material?.AveragePrice ?? 0);

        /// <summary>
        /// اسم العرض (المادة + الموقع)
        /// </summary>
        [NotMapped]
        public string DisplayName => $"{Material?.Name} - {Location?.Name}";
    }
}
