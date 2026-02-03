using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WarehouseManagement.Models.ViewModels
{
    #region Material ViewModels

    /// <summary>
    /// ViewModel لإنشاء/تعديل مادة
    /// </summary>
    public class MaterialCreateEditViewModel
    {
        public Material Material { get; set; } = new();

        /// <summary>
        /// إضافة مخزون أولي عند الإنشاء
        /// </summary>
        public bool AddInitialStock { get; set; } = false;

        /// <summary>
        /// بيانات المخزون الأولي
        /// </summary>
        public InitialStockViewModel? InitialStock { get; set; }

        // قوائم الاختيار
        public List<Category> Categories { get; set; } = new();
        public List<Location> Locations { get; set; } = new();
    }

    /// <summary>
    /// ViewModel للمخزون الأولي
    /// </summary>
    public class InitialStockViewModel
    {
        [Required(ErrorMessage = "الموقع مطلوب")]
        [Display(Name = "الموقع")]
        public int LocationId { get; set; }

        [Required(ErrorMessage = "الكمية مطلوبة")]
        [Range(0, int.MaxValue)]
        [Display(Name = "الكمية")]
        public int Quantity { get; set; }

        [Display(Name = "السعر")]
        public decimal? UnitPrice { get; set; }

        [Display(Name = "تاريخ انتهاء الصلاحية")]
        public DateTime? ExpiryDate { get; set; }
    }

    /// <summary>
    /// ViewModel لعرض المادة مع تفاصيلها
    /// </summary>
    public class MaterialDetailsViewModel
    {
        public Material Material { get; set; } = null!;
        public List<MaterialStock> Stocks { get; set; } = new();
        public List<Purchase> RecentPurchases { get; set; } = new();
        public List<InventoryRecord> RecentInventories { get; set; } = new();
        public List<Transfer> RecentTransfers { get; set; } = new();

        // إحصائيات
        public int TotalQuantity { get; set; }
        public decimal TotalValue { get; set; }
        public int LocationsCount { get; set; }
        public int PurchasesCount { get; set; }
    }

    /// <summary>
    /// ViewModel للبحث في المواد
    /// </summary>
    public class MaterialSearchViewModel
    {
        [Display(Name = "البحث")]
        public string? SearchTerm { get; set; }

        [Display(Name = "الفئة")]
        public int? CategoryId { get; set; }

        [Display(Name = "الموقع")]
        public int? LocationId { get; set; }

        [Display(Name = "نوع الموقع")]
        public LocationType? LocationType { get; set; }

        [Display(Name = "المخزون المنخفض فقط")]
        public bool LowStockOnly { get; set; } = false;

        [Display(Name = "تضمين غير النشطة")]
        public bool IncludeInactive { get; set; } = false;

        // النتائج
        public List<Material> Results { get; set; } = new();

        // قوائم الاختيار
        public List<Category> Categories { get; set; } = new();
        public List<Location> Locations { get; set; } = new();
    }

    #endregion

    #region Stock ViewModels

    /// <summary>
    /// ViewModel لإضافة/تعديل المخزون
    /// </summary>
    public class StockCreateEditViewModel
    {
        public MaterialStock Stock { get; set; } = new();
        public List<Material> Materials { get; set; } = new();
        public List<Location> Locations { get; set; } = new();
    }

    /// <summary>
    /// ViewModel لتعديل كمية المخزون
    /// </summary>
    public class StockAdjustmentViewModel
    {
        [Required]
        public int StockId { get; set; }

        public MaterialStock? Stock { get; set; }

        /// <summary>
        /// اسم المادة
        /// </summary>
        [Display(Name = "المادة")]
        public string MaterialName { get; set; } = string.Empty;

        /// <summary>
        /// اسم الموقع
        /// </summary>
        [Display(Name = "الموقع")]
        public string LocationName { get; set; } = string.Empty;

        /// <summary>
        /// الكمية الحالية
        /// </summary>
        [Display(Name = "الكمية الحالية")]
        public int CurrentQuantity { get; set; }

        [Required]
        [Display(Name = "نوع التعديل")]
        public StockAdjustmentType AdjustmentType { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        [Display(Name = "الكمية")]
        public int Quantity { get; set; }

        [Required]
        [Display(Name = "السبب")]
        public string Reason { get; set; } = string.Empty;

        [Display(Name = "ملاحظات")]
        public string? Notes { get; set; }
    }

    public enum StockAdjustmentType
    {
        [Display(Name = "إضافة")]
        Add = 1,

        [Display(Name = "خصم")]
        Deduct = 2,

        [Display(Name = "تصحيح (تحديد الكمية)")]
        Set = 3
    }

    #endregion

    #region Dashboard ViewModels

    /// <summary>
    /// ViewModel للوحة التحكم
    /// </summary>
    public class DashboardViewModel
    {
        public int TotalMaterials { get; set; }
        public int TotalLocations { get; set; }
        public int TotalCategories { get; set; }
        public int LowStockItems { get; set; }
        public int ExpiringItems { get; set; }
        public decimal TotalValue { get; set; }
        public int PendingTransfers { get; set; }
        public int TotalInventoryRecords { get; set; }
        public int TotalConsumptionRecords { get; set; }

        public List<Transfer> RecentTransfers { get; set; } = new();
        public List<MaterialStock> CriticalItems { get; set; } = new();
        public List<Purchase> RecentPurchases { get; set; } = new();

        // إحصائيات الرسوم البيانية
        public Dictionary<string, int> MaterialsByCategory { get; set; } = new();
        public Dictionary<string, int> MaterialsByLocation { get; set; } = new();
        public Dictionary<string, decimal> ValueByCategory { get; set; } = new();
    }

    #endregion

    #region Report ViewModels

    /// <summary>
    /// ViewModel لتقرير الموقع (تفاصيل موقع واحد)
    /// </summary>
    public class LocationDetailReportViewModel
    {
        public Location Location { get; set; } = null!;
        public List<MaterialStock> Stocks { get; set; } = new();
        public int TotalItems { get; set; }
        public int TotalQuantity { get; set; }
        public decimal TotalValue { get; set; }
        public DateTime GeneratedDate { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// ViewModel لتقرير الأسعار (تفصيل مادة واحدة)
    /// </summary>
    public class PricingDetailReportViewModel
    {
        public Material Material { get; set; } = null!;
        public List<Purchase> Purchases { get; set; } = new();
        public int TotalQuantityPurchased { get; set; }
        public decimal TotalAmountSpent { get; set; }
        public decimal AveragePrice { get; set; }
        public decimal CurrentValue { get; set; }
        public DateTime? FirstPurchaseDate { get; set; }
        public DateTime? LastPurchaseDate { get; set; }
    }

    #endregion
}