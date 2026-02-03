using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WarehouseManagement.Models
{
    /// <summary>
    /// المادة - النموذج الأساسي والمصدر الوحيد لبيانات المواد
    /// Material - The single source of truth for material data
    /// </summary>
    public class Material
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// اسم المادة
        /// </summary>
        [Required(ErrorMessage = "اسم المادة مطلوب")]
        [StringLength(200)]
        [Display(Name = "اسم المادة")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// رمز المادة / رقم الترميز (فريد)
        /// مثل: 1/1/1, 2/1/1, COMP-001
        /// </summary>
        [Required(ErrorMessage = "رمز المادة مطلوب")]
        [StringLength(50)]
        [Display(Name = "رمز المادة")]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// وصف المادة (حديد/خشب/ألمنيوم/بلاستيك/معدن)
        /// </summary>
        [StringLength(500)]
        [Display(Name = "الوصف")]
        public string? Description { get; set; }

        /// <summary>
        /// نوع المادة (حديد/خشب/ألمنيوم/بلاستيك/معدن)
        /// </summary>
        [StringLength(100)]
        [Display(Name = "نوع المادة")]
        public string? MaterialType { get; set; }

        /// <summary>
        /// المواصفات الفنية
        /// </summary>
        [StringLength(1000)]
        [Display(Name = "المواصفات")]
        public string? Specifications { get; set; }

        /// <summary>
        /// وحدة القياس
        /// </summary>
        [Required(ErrorMessage = "وحدة القياس مطلوبة")]
        [StringLength(50)]
        [Display(Name = "الوحدة")]
        public string Unit { get; set; } = "قطعة";

        /// <summary>
        /// الفئة
        /// </summary>
        [Display(Name = "الفئة")]
        public int? CategoryId { get; set; }

        /// <summary>
        /// الحد الأدنى للمخزون (للتنبيه)
        /// </summary>
        [Display(Name = "الحد الأدنى")]
        public int MinimumStock { get; set; } = 5;

        /// <summary>
        /// هل المادة نشطة (غير محذوفة)
        /// </summary>
        [Display(Name = "نشط")]
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// العائدية / الجهة المالكة
        /// </summary>
        [StringLength(200)]
        [Display(Name = "العائدية")]
        public string? Ownership { get; set; }

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
        /// الفئة
        /// </summary>
        public virtual Category? Category { get; set; }

        /// <summary>
        /// المخزون في المواقع المختلفة
        /// </summary>
        public virtual ICollection<MaterialStock> Stocks { get; set; } = new List<MaterialStock>();

        /// <summary>
        /// سجلات الشراء
        /// </summary>
        public virtual ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();

        /// <summary>
        /// سجلات الجرد
        /// </summary>
        public virtual ICollection<InventoryRecord> InventoryRecords { get; set; } = new List<InventoryRecord>();

        /// <summary>
        /// عمليات النقل
        /// </summary>
        public virtual ICollection<Transfer> Transfers { get; set; } = new List<Transfer>();

        // ============ Computed Properties ============

        /// <summary>
        /// إجمالي الكمية في جميع المواقع
        /// </summary>
        [NotMapped]
        [Display(Name = "إجمالي الكمية")]
        public int TotalQuantity => Stocks?.Sum(s => s.Quantity) ?? 0;

        /// <summary>
        /// آخر سعر شراء
        /// </summary>
        [NotMapped]
        [Display(Name = "آخر سعر")]
        public decimal? LastPurchasePrice => Purchases?
            .OrderByDescending(p => p.PurchaseDate)
            .FirstOrDefault()?.UnitPrice;

        /// <summary>
        /// متوسط سعر الشراء
        /// </summary>
        [NotMapped]
        [Display(Name = "متوسط السعر")]
        public decimal AveragePrice
        {
            get
            {
                var purchases = Purchases?.Where(p => p.Quantity > 0).ToList();
                if (purchases == null || !purchases.Any()) return 0;

                var totalQty = purchases.Sum(p => p.Quantity);
                var totalValue = purchases.Sum(p => p.TotalPrice);
                return totalQty > 0 ? totalValue / totalQty : 0;
            }
        }

        /// <summary>
        /// القيمة الإجمالية للمخزون
        /// </summary>
        [NotMapped]
        [Display(Name = "القيمة الإجمالية")]
        public decimal TotalValue => TotalQuantity * (LastPurchasePrice ?? AveragePrice);

        /// <summary>
        /// هل المخزون منخفض
        /// </summary>
        [NotMapped]
        public bool IsLowStock => TotalQuantity <= MinimumStock;
    }
}