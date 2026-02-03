using System;
using System.ComponentModel.DataAnnotations;

namespace WarehouseManagement.Models
{
    /// <summary>
    /// عملية نقل المواد بين المواقع
    /// </summary>
    public class Transfer
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "المادة")]
        public int MaterialId { get; set; }

        [Required]
        [Display(Name = "من الموقع")]
        public int FromLocationId { get; set; }

        [Required]
        [Display(Name = "إلى الموقع")]
        public int ToLocationId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "الكمية يجب أن تكون أكبر من صفر")]
        [Display(Name = "الكمية")]
        public int Quantity { get; set; }

        [Required]
        [Display(Name = "تاريخ النقل")]
        public DateTime TransferDate { get; set; } = DateTime.Now;

        [StringLength(500)]
        [Display(Name = "السبب")]
        public string? Reason { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "المسؤول عن النقل")]
        public string TransferredBy { get; set; } = string.Empty;

        [Display(Name = "تم التأكيد")]
        public bool IsConfirmed { get; set; } = false;

        [Display(Name = "تاريخ التأكيد")]
        public DateTime? ConfirmedDate { get; set; }

        [StringLength(100)]
        [Display(Name = "تم التأكيد بواسطة")]
        public string? ConfirmedBy { get; set; }

        /// <summary>
        /// هل تم تنفيذ النقل فعلياً (تحديث المخزون)
        /// </summary>
        [Display(Name = "تم التنفيذ")]
        public bool IsExecuted { get; set; } = false;

        [Display(Name = "تاريخ التنفيذ")]
        public DateTime? ExecutedDate { get; set; }

        /// <summary>
        /// هل تم إلغاء طلب النقل
        /// </summary>
        [Display(Name = "ملغي")]
        public bool IsCancelled { get; set; } = false;

        [StringLength(500)]
        [Display(Name = "ملاحظات")]
        public string? Notes { get; set; }

        [Display(Name = "تاريخ الإنشاء")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation Properties
        public virtual Material Material { get; set; } = null!;
        public virtual Location FromLocation { get; set; } = null!;
        public virtual Location ToLocation { get; set; } = null!;
    }
}