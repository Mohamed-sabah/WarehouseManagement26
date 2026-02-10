using ClosedXML.Excel;
using WarehouseManagement.Models;
using WarehouseManagement.Models.ViewModels;

namespace WarehouseManagement.Services
{
    public interface IExcelExportService
    {
        byte[] ExportMaterialsReport(List<Material> materials, string title);
        byte[] ExportInventoryForm2(InventoryForm2ViewModel model);
        byte[] ExportConsumptionForm5(ConsumptionForm5ViewModel model);
        byte[] ExportPurchasesYearly(PurchasesYearlyReportViewModel model);
        byte[] ExportLowStockReport(LowStockReportViewModel model);
        byte[] ExportExpiryReport(ExpiryReportViewModel model);
        byte[] ExportPricingReport(PricingReportViewModel model);
        byte[] ExportLocationReport(LocationReportViewModel model);
        byte[] ExportMaterialsValue(MaterialsValueReportViewModel model);
    }

    public class ExcelExportService : IExcelExportService
    {
        public byte[] ExportMaterialsReport(List<Material> materials, string title)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("تقرير المواد");

            // العنوان
            worksheet.Cell(1, 1).Value = title;
            worksheet.Range(1, 1, 1, 8).Merge().Style
                .Font.SetBold(true)
                .Font.SetFontSize(16)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // رؤوس الأعمدة
            var headers = new[] { "ت", "رمز المادة", "اسم المادة", "الفئة", "الوحدة", "الكمية", "السعر", "القيمة الإجمالية" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(3, i + 1).Value = headers[i];
                worksheet.Cell(3, i + 1).Style
                    .Font.SetBold(true)
                    .Fill.SetBackgroundColor(XLColor.LightGray)
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            }

            // البيانات
            int row = 4;
            int seq = 1;
            foreach (var material in materials)
            {
                worksheet.Cell(row, 1).Value = seq++;
                worksheet.Cell(row, 2).Value = material.Code;
                worksheet.Cell(row, 3).Value = material.Name;
                worksheet.Cell(row, 4).Value = material.Category?.Name ?? "-";
                worksheet.Cell(row, 5).Value = material.Unit;
                worksheet.Cell(row, 6).Value = material.TotalQuantity;
                worksheet.Cell(row, 7).Value = material.AveragePrice;
                worksheet.Cell(row, 8).Value = material.TotalValue;

                for (int col = 1; col <= 8; col++)
                    worksheet.Cell(row, col).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);

                row++;
            }

            // تنسيق الأرقام
            worksheet.Column(7).Style.NumberFormat.Format = "#,##0";
            worksheet.Column(8).Style.NumberFormat.Format = "#,##0";

            // الإجماليات
            worksheet.Cell(row, 5).Value = "الإجمالي:";
            worksheet.Cell(row, 5).Style.Font.SetBold(true);
            worksheet.Cell(row, 6).FormulaA1 = $"=SUM(F4:F{row - 1})";
            worksheet.Cell(row, 8).FormulaA1 = $"=SUM(H4:H{row - 1})";
            worksheet.Range(row, 1, row, 8).Style.Font.SetBold(true);

            // تعديل عرض الأعمدة
            worksheet.Columns().AdjustToContents();
            worksheet.RightToLeft = true;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public byte[] ExportInventoryForm2(InventoryForm2ViewModel model)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("نموذج 2");

            // العنوان
            worksheet.Cell(1, 1).Value = "نموذج رقم (2)";
            worksheet.Range(1, 1, 1, 10).Merge().Style
                .Font.SetBold(true)
                .Font.SetFontSize(18)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            worksheet.Cell(2, 1).Value = $"قائمة بموجودات المخازن لسنة {model.Year}";
            worksheet.Range(2, 1, 2, 10).Merge().Style
                .Font.SetFontSize(14)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // معلومات المؤسسة
            worksheet.Cell(4, 1).Value = "اسم المؤسسة:";
            worksheet.Cell(4, 2).Value = model.InstitutionName;
            worksheet.Cell(4, 5).Value = "اسم المخزن:";
            worksheet.Cell(4, 6).Value = model.WarehouseName;
            worksheet.Cell(5, 1).Value = "تاريخ الجرد:";
            worksheet.Cell(5, 2).Value = model.InventoryDate.ToString("yyyy/MM/dd");
            worksheet.Cell(5, 5).Value = "رقم النموذج:";
            worksheet.Cell(5, 6).Value = model.FormNumber;

            // رؤوس الأعمدة
            var headers = new[] { "ت", "رمز المادة", "اسم المادة", "الوحدة", "الكمية الدفترية", "القيمة الدفترية", "الكمية الفعلية", "القيمة الفعلية", "الفرق", "ملاحظات" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(7, i + 1).Value = headers[i];
                worksheet.Cell(7, i + 1).Style
                    .Font.SetBold(true)
                    .Fill.SetBackgroundColor(XLColor.LightGray)
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin)
                    .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
            }

            // البيانات
            int row = 8;
            int seq = 1;
            foreach (var item in model.Items)
            {
                worksheet.Cell(row, 1).Value = seq++;
                worksheet.Cell(row, 2).Value = item.MaterialCode;
                worksheet.Cell(row, 3).Value = item.MaterialName;
                worksheet.Cell(row, 4).Value = item.Unit;
                worksheet.Cell(row, 5).Value = item.BookQuantity;
                worksheet.Cell(row, 6).Value = item.BookValue;
                worksheet.Cell(row, 7).Value = item.ActualQuantity;
                worksheet.Cell(row, 8).Value = item.ActualValue;
                worksheet.Cell(row, 9).Value = item.Variance;
                worksheet.Cell(row, 10).Value = item.Notes ?? "";

                // تلوين الفروقات
                if (item.Variance < 0)
                    worksheet.Cell(row, 9).Style.Font.SetFontColor(XLColor.Red);
                else if (item.Variance > 0)
                    worksheet.Cell(row, 9).Style.Font.SetFontColor(XLColor.Green);

                for (int col = 1; col <= 10; col++)
                    worksheet.Cell(row, col).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);

                row++;
            }

            // الإجماليات
            worksheet.Cell(row, 4).Value = "الإجمالي:";
            worksheet.Cell(row, 4).Style.Font.SetBold(true);
            worksheet.Cell(row, 5).FormulaA1 = $"=SUM(E8:E{row - 1})";
            worksheet.Cell(row, 6).FormulaA1 = $"=SUM(F8:F{row - 1})";
            worksheet.Cell(row, 7).FormulaA1 = $"=SUM(G8:G{row - 1})";
            worksheet.Cell(row, 8).FormulaA1 = $"=SUM(H8:H{row - 1})";
            worksheet.Range(row, 1, row, 10).Style.Font.SetBold(true);

            // التواقيع
            row += 3;
            worksheet.Cell(row, 1).Value = "أمين المخزن:";
            worksheet.Cell(row, 3).Value = model.WarehouseKeeper;
            worksheet.Cell(row, 6).Value = "رئيس لجنة الجرد:";
            worksheet.Cell(row, 8).Value = model.CommitteeHead;

            // تنسيق الأرقام
            worksheet.Column(6).Style.NumberFormat.Format = "#,##0";
            worksheet.Column(8).Style.NumberFormat.Format = "#,##0";

            worksheet.Columns().AdjustToContents();
            worksheet.RightToLeft = true;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public byte[] ExportConsumptionForm5(ConsumptionForm5ViewModel model)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("نموذج 5");

            // العنوان
            worksheet.Cell(1, 1).Value = "نموذج رقم (5)";
            worksheet.Range(1, 1, 1, 10).Merge().Style
                .Font.SetBold(true)
                .Font.SetFontSize(18)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            worksheet.Cell(2, 1).Value = $"قائمة بالموجودات المستهلكة - {model.Month}/{model.Year}";
            worksheet.Range(2, 1, 2, 10).Merge().Style
                .Font.SetFontSize(14)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // معلومات المؤسسة
            worksheet.Cell(4, 1).Value = "اسم المؤسسة:";
            worksheet.Cell(4, 2).Value = model.InstitutionName;
            worksheet.Cell(4, 5).Value = "رقم النموذج:";
            worksheet.Cell(4, 6).Value = model.FormNumber;

            // رؤوس الأعمدة
            var headers = new[] { "ت", "التاريخ", "رقم السند", "رمز المادة", "اسم المادة", "الوحدة", "الكمية", "السعر", "القيمة", "ملاحظات" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(6, i + 1).Value = headers[i];
                worksheet.Cell(6, i + 1).Style
                    .Font.SetBold(true)
                    .Fill.SetBackgroundColor(XLColor.LightGray)
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            }

            // البيانات
            int row = 7;
            int seq = 1;
            foreach (var item in model.Items)
            {
                worksheet.Cell(row, 1).Value = seq++;
                worksheet.Cell(row, 2).Value = item.IssueDate.ToString("yyyy/MM/dd");
                worksheet.Cell(row, 3).Value = item.IssueNumber;
                worksheet.Cell(row, 4).Value = item.MaterialCode;
                worksheet.Cell(row, 5).Value = item.MaterialName;
                worksheet.Cell(row, 6).Value = item.Unit;
                worksheet.Cell(row, 7).Value = item.Quantity;
                worksheet.Cell(row, 8).Value = item.UnitPrice;
                worksheet.Cell(row, 9).Value = item.TotalValue;
                worksheet.Cell(row, 10).Value = item.Notes ?? "";

                for (int col = 1; col <= 10; col++)
                    worksheet.Cell(row, col).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);

                row++;
            }

            // الإجماليات
            worksheet.Cell(row, 6).Value = "الإجمالي:";
            worksheet.Cell(row, 6).Style.Font.SetBold(true);
            worksheet.Cell(row, 7).FormulaA1 = $"=SUM(G7:G{row - 1})";
            worksheet.Cell(row, 9).FormulaA1 = $"=SUM(I7:I{row - 1})";
            worksheet.Range(row, 1, row, 10).Style.Font.SetBold(true);

            worksheet.Column(8).Style.NumberFormat.Format = "#,##0";
            worksheet.Column(9).Style.NumberFormat.Format = "#,##0";

            worksheet.Columns().AdjustToContents();
            worksheet.RightToLeft = true;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public byte[] ExportPurchasesYearly(PurchasesYearlyReportViewModel model)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("المشتريات السنوية");

            // العنوان
            worksheet.Cell(1, 1).Value = $"تقرير المشتريات السنوية - {model.Year}";
            worksheet.Range(1, 1, 1, 6).Merge().Style
                .Font.SetBold(true)
                .Font.SetFontSize(16)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            // ملخص
            worksheet.Cell(3, 1).Value = "إجمالي المشتريات:";
            worksheet.Cell(3, 2).Value = model.TotalPurchases;
            worksheet.Cell(3, 2).Style.NumberFormat.Format = "#,##0";
            worksheet.Cell(4, 1).Value = "عدد الطلبات:";
            worksheet.Cell(4, 2).Value = model.TotalOrders;
            worksheet.Cell(5, 1).Value = "عدد الموردين:";
            worksheet.Cell(5, 2).Value = model.TotalSuppliers;

            // رؤوس أعمدة البيانات الشهرية
            var monthHeaders = new[] { "الشهر", "عدد الطلبات", "إجمالي المبلغ" };
            for (int i = 0; i < monthHeaders.Length; i++)
            {
                worksheet.Cell(7, i + 1).Value = monthHeaders[i];
                worksheet.Cell(7, i + 1).Style
                    .Font.SetBold(true)
                    .Fill.SetBackgroundColor(XLColor.LightGray)
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            }

            // البيانات الشهرية
            var arabicMonths = new[] { "كانون الثاني", "شباط", "آذار", "نيسان", "أيار", "حزيران", "تموز", "آب", "أيلول", "تشرين الأول", "تشرين الثاني", "كانون الأول" };
            int row = 8;
            foreach (var monthData in model.MonthlyData)
            {
                worksheet.Cell(row, 1).Value = arabicMonths[monthData.Month - 1];
                worksheet.Cell(row, 2).Value = monthData.OrderCount;
                worksheet.Cell(row, 3).Value = monthData.TotalAmount;

                for (int col = 1; col <= 3; col++)
                    worksheet.Cell(row, col).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);

                row++;
            }

            worksheet.Column(3).Style.NumberFormat.Format = "#,##0";
            worksheet.Columns().AdjustToContents();
            worksheet.RightToLeft = true;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public byte[] ExportLowStockReport(LowStockReportViewModel model)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("المخزون المنخفض");

            worksheet.Cell(1, 1).Value = "تقرير المخزون المنخفض";
            worksheet.Range(1, 1, 1, 7).Merge().Style
                .Font.SetBold(true)
                .Font.SetFontSize(16)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            var headers = new[] { "ت", "رمز المادة", "اسم المادة", "الفئة", "الموقع", "الكمية الحالية", "الحد الأدنى", "النقص" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(3, i + 1).Value = headers[i];
                worksheet.Cell(3, i + 1).Style
                    .Font.SetBold(true)
                    .Fill.SetBackgroundColor(XLColor.LightGray)
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            }

            int row = 4;
            int seq = 1;
            foreach (var item in model.Items)
            {
                worksheet.Cell(row, 1).Value = seq++;
                worksheet.Cell(row, 2).Value = item.MaterialCode;
                worksheet.Cell(row, 3).Value = item.MaterialName;
                worksheet.Cell(row, 4).Value = item.CategoryName;
                worksheet.Cell(row, 5).Value = item.LocationName;
                worksheet.Cell(row, 6).Value = item.CurrentQuantity;
                worksheet.Cell(row, 7).Value = item.MinimumStock;
                worksheet.Cell(row, 8).Value = item.ShortageQuantity;

                if (item.CurrentQuantity == 0)
                    worksheet.Row(row).Style.Font.SetFontColor(XLColor.Red);

                for (int col = 1; col <= 8; col++)
                    worksheet.Cell(row, col).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);

                row++;
            }

            worksheet.Columns().AdjustToContents();
            worksheet.RightToLeft = true;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public byte[] ExportExpiryReport(ExpiryReportViewModel model)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("صلاحية المواد");

            worksheet.Cell(1, 1).Value = "تقرير انتهاء الصلاحية";
            worksheet.Range(1, 1, 1, 8).Merge().Style
                .Font.SetBold(true)
                .Font.SetFontSize(16)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            var headers = new[] { "ت", "رمز المادة", "اسم المادة", "الموقع", "الكمية", "تاريخ الانتهاء", "الأيام المتبقية", "القيمة" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(3, i + 1).Value = headers[i];
                worksheet.Cell(3, i + 1).Style
                    .Font.SetBold(true)
                    .Fill.SetBackgroundColor(XLColor.LightGray)
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            }

            int row = 4;
            int seq = 1;
            foreach (var item in model.Items)
            {
                worksheet.Cell(row, 1).Value = seq++;
                worksheet.Cell(row, 2).Value = item.MaterialCode;
                worksheet.Cell(row, 3).Value = item.MaterialName;
                worksheet.Cell(row, 4).Value = item.LocationName;
                worksheet.Cell(row, 5).Value = item.Quantity;
                worksheet.Cell(row, 6).Value = item.ExpiryDate?.ToString("yyyy/MM/dd") ?? "-";
                worksheet.Cell(row, 7).Value = item.DaysRemaining;
                worksheet.Cell(row, 8).Value = item.TotalValue;

                if (item.DaysRemaining <= 0)
                    worksheet.Row(row).Style.Font.SetFontColor(XLColor.Red);
                else if (item.DaysRemaining <= 30)
                    worksheet.Row(row).Style.Font.SetFontColor(XLColor.Orange);

                for (int col = 1; col <= 8; col++)
                    worksheet.Cell(row, col).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);

                row++;
            }

            worksheet.Column(8).Style.NumberFormat.Format = "#,##0";
            worksheet.Columns().AdjustToContents();
            worksheet.RightToLeft = true;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public byte[] ExportPricingReport(PricingReportViewModel model)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("الأسعار");

            worksheet.Cell(1, 1).Value = "تقرير الأسعار";
            worksheet.Range(1, 1, 1, 7).Merge().Style
                .Font.SetBold(true)
                .Font.SetFontSize(16)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            var headers = new[] { "ت", "رمز المادة", "اسم المادة", "الفئة", "الوحدة", "سعر الشراء", "آخر شراء" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(3, i + 1).Value = headers[i];
                worksheet.Cell(3, i + 1).Style
                    .Font.SetBold(true)
                    .Fill.SetBackgroundColor(XLColor.LightGray)
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            }

            int row = 4;
            int seq = 1;
            foreach (var item in model.Items)
            {
                worksheet.Cell(row, 1).Value = seq++;
                worksheet.Cell(row, 2).Value = item.MaterialCode;
                worksheet.Cell(row, 3).Value = item.MaterialName;
                worksheet.Cell(row, 4).Value = item.CategoryName;
                worksheet.Cell(row, 5).Value = item.Unit;
                worksheet.Cell(row, 6).Value = item.PurchasePrice;
                worksheet.Cell(row, 7).Value = item.LastPurchaseDate?.ToString("yyyy/MM/dd") ?? "-";

                for (int col = 1; col <= 7; col++)
                    worksheet.Cell(row, col).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);

                row++;
            }

            worksheet.Column(6).Style.NumberFormat.Format = "#,##0";
            worksheet.Columns().AdjustToContents();
            worksheet.RightToLeft = true;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public byte[] ExportLocationReport(LocationReportViewModel model)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("تقرير المواقع");

            worksheet.Cell(1, 1).Value = "تقرير المواقع";
            worksheet.Range(1, 1, 1, 6).Merge().Style
                .Font.SetBold(true)
                .Font.SetFontSize(16)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            var headers = new[] { "ت", "رمز الموقع", "اسم الموقع", "عدد المواد", "إجمالي الكمية", "إجمالي القيمة" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(3, i + 1).Value = headers[i];
                worksheet.Cell(3, i + 1).Style
                    .Font.SetBold(true)
                    .Fill.SetBackgroundColor(XLColor.LightGray)
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            }

            int row = 4;
            int seq = 1;
            foreach (var loc in model.Locations)
            {
                worksheet.Cell(row, 1).Value = seq++;
                worksheet.Cell(row, 2).Value = loc.LocationCode;
                worksheet.Cell(row, 3).Value = loc.LocationName;
                worksheet.Cell(row, 4).Value = loc.MaterialsCount;
                worksheet.Cell(row, 5).Value = loc.TotalQuantity;
                worksheet.Cell(row, 6).Value = loc.TotalValue;

                for (int col = 1; col <= 6; col++)
                    worksheet.Cell(row, col).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);

                row++;
            }

            worksheet.Column(6).Style.NumberFormat.Format = "#,##0";
            worksheet.Columns().AdjustToContents();
            worksheet.RightToLeft = true;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public byte[] ExportMaterialsValue(MaterialsValueReportViewModel model)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("قيمة المواد");

            worksheet.Cell(1, 1).Value = "تقرير قيمة المواد";
            worksheet.Range(1, 1, 1, 7).Merge().Style
                .Font.SetBold(true)
                .Font.SetFontSize(16)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

            var headers = new[] { "ت", "رمز المادة", "اسم المادة", "الفئة", "الكمية", "سعر الوحدة", "القيمة الإجمالية" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(3, i + 1).Value = headers[i];
                worksheet.Cell(3, i + 1).Style
                    .Font.SetBold(true)
                    .Fill.SetBackgroundColor(XLColor.LightGray)
                    .Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            }

            int row = 4;
            int seq = 1;
            foreach (var item in model.Items)
            {
                worksheet.Cell(row, 1).Value = seq++;
                worksheet.Cell(row, 2).Value = item.MaterialCode;
                worksheet.Cell(row, 3).Value = item.MaterialName;
                worksheet.Cell(row, 4).Value = item.CategoryName;
                worksheet.Cell(row, 5).Value = item.Quantity;
                worksheet.Cell(row, 6).Value = item.UnitPrice;
                worksheet.Cell(row, 7).Value = item.TotalValue;

                for (int col = 1; col <= 7; col++)
                    worksheet.Cell(row, col).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);

                row++;
            }

            // الإجماليات
            worksheet.Cell(row, 4).Value = "الإجمالي:";
            worksheet.Cell(row, 4).Style.Font.SetBold(true);
            worksheet.Cell(row, 5).FormulaA1 = $"=SUM(E4:E{row - 1})";
            worksheet.Cell(row, 7).FormulaA1 = $"=SUM(G4:G{row - 1})";

            worksheet.Column(6).Style.NumberFormat.Format = "#,##0";
            worksheet.Column(7).Style.NumberFormat.Format = "#,##0";
            worksheet.Columns().AdjustToContents();
            worksheet.RightToLeft = true;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}
