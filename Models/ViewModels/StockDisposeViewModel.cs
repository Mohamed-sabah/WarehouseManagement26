using System.ComponentModel.DataAnnotations;

namespace WarehouseManagement.Models.ViewModels
{
    public class StockDisposeViewModel
    {
        [Required]
        public int StockId { get; set; }

        public string MaterialName { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public int CurrentQuantity { get; set; }

        [Required(ErrorMessage = "الكمية مطلوبة")]
        [Range(1, int.MaxValue, ErrorMessage = "الكمية يجب أن تكون أكبر من صفر")]
        [Display(Name = "الكمية المراد إتلافها")]
        public int DisposeQuantity { get; set; }

        [Required(ErrorMessage = "سبب الإتلاف مطلوب")]
        [Display(Name = "سبب الإتلاف")]
        public string DisposeReason { get; set; } = string.Empty;

        [Display(Name = "ملاحظات")]
        public string? Notes { get; set; }

        [Display(Name = "المخول بالإتلاف")]
        public string? AuthorizedBy { get; set; }
    }
}
