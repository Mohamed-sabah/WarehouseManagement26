using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WarehouseManagement.Models
{
    /// <summary>
    /// فئة المواد
    /// </summary>
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم الفئة مطلوب")]
        [StringLength(100)]
        [Display(Name = "اسم الفئة")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "الوصف")]
        public string? Description { get; set; }

        /// <summary>
        /// رمز الفئة
        /// </summary>
        [StringLength(20)]
        [Display(Name = "رمز الفئة")]
        public string? Code { get; set; }

        /// <summary>
        /// الفئة الأم (للتصنيف الهرمي)
        /// </summary>
        [Display(Name = "الفئة الأم")]
        public int? ParentCategoryId { get; set; }

        /// <summary>
        /// هل الفئة نشطة
        /// </summary>
        [Display(Name = "نشط")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "تاريخ الإنشاء")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation Properties
        public virtual Category? ParentCategory { get; set; }
        public virtual ICollection<Category> SubCategories { get; set; } = new List<Category>();
        public virtual ICollection<Material> Materials { get; set; } = new List<Material>();
    }
}
