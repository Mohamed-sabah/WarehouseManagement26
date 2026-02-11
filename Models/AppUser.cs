using System;
using System.ComponentModel.DataAnnotations;

namespace WarehouseManagement.Models
{
    /// <summary>
    /// مستخدم النظام
    /// </summary>
    public class AppUser
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "اسم المستخدم مطلوب")]
        [StringLength(50)]
        [Display(Name = "اسم المستخدم")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [Display(Name = "كلمة المرور المشفرة")]
        public string PasswordHash { get; set; } = string.Empty;

        [Required(ErrorMessage = "الاسم الكامل مطلوب")]
        [StringLength(100)]
        [Display(Name = "الاسم الكامل")]
        public string FullName { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "البريد الإلكتروني")]
        public string? Email { get; set; }

        [Required]
        [Display(Name = "الدور")]
        public UserRole Role { get; set; } = UserRole.Viewer;

        [StringLength(200)]
        [Display(Name = "القسم")]
        public string? Department { get; set; }

        [Display(Name = "نشط")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "آخر دخول")]
        public DateTime? LastLoginDate { get; set; }

        [Display(Name = "تاريخ الإنشاء")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }

    public enum UserRole
    {
        [Display(Name = "مدير النظام")]
        Admin = 1,

        [Display(Name = "أمين المخزن")]
        StoreKeeper = 2,

        [Display(Name = "محاسب")]
        Accountant = 3,

        [Display(Name = "مشاهد")]
        Viewer = 4
    }
}
