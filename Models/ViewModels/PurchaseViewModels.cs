using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WarehouseManagement.Models.ViewModels
{
    /// <summary>
    /// ViewModel لإنشاء/تعديل عملية شراء
    /// </summary>
    public class PurchaseCreateEditViewModel
    {
        public Purchase Purchase { get; set; } = new();

        /// <summary>
        /// إضافة مادة جديدة أثناء الشراء
        /// </summary>
        [Display(Name = "إضافة مادة جديدة")]
        public bool CreateNewMaterial { get; set; } = false;

        /// <summary>
        /// بيانات المادة الجديدة
        /// </summary>
        public NewMaterialViewModel? NewMaterial { get; set; }

        /// <summary>
        /// إضافة للمخزون تلقائياً
        /// </summary>
        [Display(Name = "إضافة للمخزون تلقائياً")]
        public bool AddToStockAutomatically { get; set; } = true;

        // قوائم الاختيار
        public List<Material> Materials { get; set; } = new();
        public List<Location> Locations { get; set; } = new();
    }

    /// <summary>
    /// ViewModel لإنشاء مادة جديدة أثناء الشراء
    /// </summary>
    public class NewMaterialViewModel
    {
        [Required(ErrorMessage = "اسم المادة مطلوب")]
        [StringLength(200)]
        [Display(Name = "اسم المادة")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "رمز المادة مطلوب")]
        [StringLength(50)]
        [Display(Name = "رمز المادة")]
        public string Code { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "الوصف")]
        public string? Description { get; set; }

        [Required]
        [Display(Name = "الفئة")]
        public int CategoryId { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "الوحدة")]
        public string Unit { get; set; } = "قطعة";
    }

    /// <summary>
    /// ViewModel لقائمة المشتريات
    /// </summary>
    public class PurchaseListViewModel
    {
        // فلاتر
        [Display(Name = "المادة")]
        public int? MaterialId { get; set; }

        [Display(Name = "الموقع")]
        public int? LocationId { get; set; }

        [Display(Name = "السنة")]
        public int? Year { get; set; }

        [Display(Name = "طريقة الحصول")]
        public AcquisitionMethod? Method { get; set; }

        [Display(Name = "من تاريخ")]
        public DateTime? FromDate { get; set; }

        [Display(Name = "إلى تاريخ")]
        public DateTime? ToDate { get; set; }

        // النتائج
        public List<Purchase> Purchases { get; set; } = new();

        // الإحصائيات
        public int TotalPurchases { get; set; }
        public int TotalQuantity { get; set; }
        public decimal TotalValue { get; set; }

        // قوائم الاختيار
        public List<Material> Materials { get; set; } = new();
        public List<Location> Locations { get; set; } = new();
        public List<int> AvailableYears { get; set; } = new();
    }

    /// <summary>
    /// ViewModel لتقرير المشتريات السنوي
    /// </summary>
    public class PurchaseYearlyReportViewModel
    {
        public int Year { get; set; }
        public List<Purchase> Purchases { get; set; } = new();

        // إحصائيات
        public int TotalPurchases { get; set; }
        public int TotalQuantity { get; set; }
        public decimal TotalValue { get; set; }
        public int UniqueMaterials { get; set; }

        // تجميع حسب الفئة
        public Dictionary<string, PurchaseCategorySummary> ByCategory { get; set; } = new();

        // تجميع حسب الشهر
        public Dictionary<int, PurchaseMonthSummary> ByMonth { get; set; } = new();

        // تجميع حسب طريقة الحصول
        public Dictionary<AcquisitionMethod, int> ByMethod { get; set; } = new();

        public List<int> AvailableYears { get; set; } = new();
        public DateTime GeneratedDate { get; set; } = DateTime.Now;
    }

    public class PurchaseCategorySummary
    {
        public string CategoryName { get; set; } = string.Empty;
        public int Count { get; set; }
        public int TotalQuantity { get; set; }
        public decimal TotalValue { get; set; }
    }

    public class PurchaseMonthSummary
    {
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public int Count { get; set; }
        public int TotalQuantity { get; set; }
        public decimal TotalValue { get; set; }
    }

    /// <summary>
    /// ViewModel لشراء متعدد المواد (دفعة واحدة)
    /// </summary>
    public class BulkPurchaseViewModel
    {
        [Required]
        [Display(Name = "تاريخ الشراء")]
        public DateTime PurchaseDate { get; set; } = DateTime.Now;

        [Display(Name = "رقم الفاتورة")]
        public string? InvoiceNumber { get; set; }

        [Display(Name = "المورد")]
        public string? Supplier { get; set; }

        [Required]
        [Display(Name = "موقع التخزين")]
        public int LocationId { get; set; }

        [Display(Name = "طريقة الحصول")]
        public AcquisitionMethod Method { get; set; } = AcquisitionMethod.Purchase;

        /// <summary>
        /// قائمة المواد المشتراة
        /// </summary>
        public List<BulkPurchaseItemViewModel> Items { get; set; } = new();

        // قوائم الاختيار
        public List<Material> Materials { get; set; } = new();
        public List<Location> Locations { get; set; } = new();
    }

    public class BulkPurchaseItemViewModel
    {
        [Required]
        [Display(Name = "المادة")]
        public int MaterialId { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        [Display(Name = "الكمية")]
        public int Quantity { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        [Display(Name = "سعر المفرد")]
        public decimal UnitPrice { get; set; }

        [Display(Name = "ملاحظات")]
        public string? Notes { get; set; }

        public bool IsSelected { get; set; } = true;
    }
}