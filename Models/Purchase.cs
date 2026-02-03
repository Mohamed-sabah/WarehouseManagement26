using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WarehouseManagement.Models
{
    /// <summary>
    /// سجل الشراء الموحد
    /// Purchase - Unified purchase record
    /// يحل محل PurchasePrice و PurchaseBatch السابقين
    /// </summary>
    public class Purchase
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// المادة المشتراة
        /// </summary>
        [Required]
        [Display(Name = "المادة")]
        public int MaterialId { get; set; }

        /// <summary>
        /// الموقع الذي ستخزن فيه المادة
        /// </summary>
        [Required]
        [Display(Name = "موقع التخزين")]
        public int LocationId { get; set; }

        /// <summary>
        /// الكمية المشتراة
        /// </summary>
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "الكمية يجب أن تكون أكبر من صفر")]
        [Display(Name = "الكمية")]
        public int Quantity { get; set; }

        /// <summary>
        /// سعر المفرد
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "سعر المفرد")]
        [DisplayFormat(DataFormatString = "{0:N0}", ApplyFormatInEditMode = false)]
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// السعر الكلي (محسوب)
        /// </summary>
        [NotMapped]
        [Display(Name = "السعر الكلي")]
        public decimal TotalPrice => Quantity * UnitPrice;

        /// <summary>
        /// السعر الكلي المخزن (للتقارير)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "السعر الكلي")]
        public decimal StoredTotalPrice { get; set; }

        /// <summary>
        /// تاريخ الشراء
        /// </summary>
        [Required]
        [Display(Name = "تاريخ الشراء")]
        [DataType(DataType.Date)]
        public DateTime PurchaseDate { get; set; } = DateTime.Now;

        /// <summary>
        /// سنة الشراء (للتقارير والفلترة)
        /// </summary>
        [Display(Name = "سنة الشراء")]
        public int PurchaseYear => PurchaseDate.Year;

        /// <summary>
        /// رقم الفاتورة
        /// </summary>
        [StringLength(50)]
        [Display(Name = "رقم الفاتورة")]
        public string? InvoiceNumber { get; set; }

        /// <summary>
        /// المورد
        /// </summary>
        [StringLength(200)]
        [Display(Name = "المورد")]
        public string? Supplier { get; set; }

        /// <summary>
        /// طريقة الحصول على المادة
        /// </summary>
        [Required]
        [Display(Name = "طريقة الحصول")]
        public AcquisitionMethod Method { get; set; } = AcquisitionMethod.Purchase;

        /// <summary>
        /// مصدر النقل (إذا كانت الطريقة مناقلة)
        /// </summary>
        [StringLength(200)]
        [Display(Name = "مصدر النقل")]
        public string? TransferSource { get; set; }

        /// <summary>
        /// العملة
        /// </summary>
        [StringLength(10)]
        [Display(Name = "العملة")]
        public string Currency { get; set; } = "IQD";

        /// <summary>
        /// سعر الصرف (إذا كانت العملة غير الدينار)
        /// </summary>
        [Column(TypeName = "decimal(18,4)")]
        [Display(Name = "سعر الصرف")]
        public decimal ExchangeRate { get; set; } = 1;

        /// <summary>
        /// السعر بالدينار العراقي
        /// </summary>
        [NotMapped]
        [Display(Name = "السعر بالدينار")]
        public decimal PriceInIQD => TotalPrice * ExchangeRate;

        /// <summary>
        /// رقم الدفعة / الباتش
        /// </summary>
        [StringLength(50)]
        [Display(Name = "رقم الدفعة")]
        public string? BatchNumber { get; set; }

        /// <summary>
        /// تاريخ انتهاء الصلاحية للدفعة
        /// </summary>
        [Display(Name = "تاريخ انتهاء الصلاحية")]
        public DateTime? ExpiryDate { get; set; }

        /// <summary>
        /// هل تم إضافة الكمية للمخزون
        /// </summary>
        [Display(Name = "تم الإضافة للمخزون")]
        public bool IsAddedToStock { get; set; } = false;

        /// <summary>
        /// تاريخ الإضافة للمخزون
        /// </summary>
        [Display(Name = "تاريخ الإضافة للمخزون")]
        public DateTime? AddedToStockDate { get; set; }

        /// <summary>
        /// ملاحظات
        /// </summary>
        [StringLength(500)]
        [Display(Name = "ملاحظات")]
        public string? Notes { get; set; }

        [Display(Name = "تاريخ الإنشاء")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [StringLength(100)]
        [Display(Name = "أنشئ بواسطة")]
        public string? CreatedBy { get; set; }

        // ============ Navigation Properties ============

        public virtual Material Material { get; set; } = null!;
        public virtual Location Location { get; set; } = null!;
    }

    /// <summary>
    /// طريقة الحصول على المادة
    /// </summary>
    public enum AcquisitionMethod
    {
        [Display(Name = "شراء")]
        Purchase = 1,

        [Display(Name = "مناقلة")]
        Transfer = 2,

        [Display(Name = "هدية")]
        Gift = 3,

        [Display(Name = "إعارة")]
        Loan = 4,

        [Display(Name = "جرد افتتاحي")]
        OpeningBalance = 5,

        [Display(Name = "أخرى")]
        Other = 6
    }
}
