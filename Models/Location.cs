using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WarehouseManagement.Models
{
    /// <summary>
    /// الموقع / المخزن
    /// </summary>
    public class Location
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم الموقع مطلوب")]
        [StringLength(100)]
        [Display(Name = "اسم الموقع")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "رمز الموقع مطلوب")]
        [StringLength(20)]
        [Display(Name = "رمز الموقع")]
        public string Code { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "الوصف")]
        public string? Description { get; set; }

        [Required]
        [Display(Name = "نوع الموقع")]
        public LocationType Type { get; set; }

        [Display(Name = "السعة القصوى")]
        public int? MaxCapacity { get; set; }

        [Display(Name = "الطابق")]
        public int? Floor { get; set; }

        [StringLength(100)]
        [Display(Name = "المبنى")]
        public string? Building { get; set; }

        /// <summary>
        /// العنوان الكامل
        /// </summary>
        [StringLength(300)]
        [Display(Name = "العنوان")]
        public string? Address { get; set; }

        /// <summary>
        /// القسم / الدائرة المسؤولة
        /// </summary>
        [StringLength(200)]
        [Display(Name = "القسم المسؤول")]
        public string? ResponsibleDepartment { get; set; }

        /// <summary>
        /// المسؤول عن الموقع
        /// </summary>
        [StringLength(100)]
        [Display(Name = "المسؤول")]
        public string? ResponsiblePerson { get; set; }

        /// <summary>
        /// هل الموقع نشط
        /// </summary>
        [Display(Name = "نشط")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "تاريخ الإنشاء")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation Properties
        public virtual ICollection<MaterialStock> Stocks { get; set; } = new List<MaterialStock>();
        public virtual ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
        public virtual ICollection<InventoryRecord> InventoryRecords { get; set; } = new List<InventoryRecord>();
    }

    /// <summary>
    /// نوع الموقع
    /// </summary>
    public enum LocationType
    {
        [Display(Name = "مخزن")]
        Warehouse = 1,

        [Display(Name = "مكتب")]
        Office = 2,

        [Display(Name = "ورشة")]
        Workshop = 3,

        [Display(Name = "مختبر")]
        Laboratory = 4,

        [Display(Name = "غرفة اجتماعات")]
        MeetingRoom = 5,

        [Display(Name = "قسم")]
        Department = 6,

        [Display(Name = "أخرى")]
        Other = 7
    }
}
