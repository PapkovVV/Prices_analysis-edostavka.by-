using ClosedXML.Excel;
using PriceAnalysis.DataBaseServices;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace PriceAnalysis.Services.ExportServices;

public class ExcelExport
{
    static int row = 1;//Строка
    static int column = 1;//Столбец (ячейка)
    public static async Task ExcelImportAndOpenAsync(DataGrid dataGrid, string name, string title, bool isNewFile)
    {
        row = 1;
        string filePathExcel = isNewFile ? $@"ExportedFiles\Excel\Exported{name}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx" :
            $@"ExportedFiles\Excel\Exported{name}.xlsx";
        const string excelPath = @"C:\Program Files\Microsoft Office\root\Office16\EXCEL.EXE";
        const string sheetName = "Sheet1";


        List<string> columnHeaders = new List<string>();//Названия столбцов
        foreach (var column in dataGrid.Columns)
        {
            if (column is DataGridTextColumn textColumn)
            {
                columnHeaders.Add(textColumn.Header.ToString());
            }
        }

        List<string> cellValues = new List<string>();

        foreach (var item in dataGrid.Items)
        {
            if (item is DataRowView row)
            {
                foreach (var cellValue in row.Row.ItemArray)
                {
                    cellValues.Add(cellValue.ToString());
                }
                cellValues.Add("end");
            }
        }



        using (var workbook = new XLWorkbook())
        {
            var worksheet = workbook.Worksheets.Add(sheetName);

            SetFileTittle(worksheet, title);//Устанавливаем заголовок файла
            SetHeaderRow(worksheet, "", false, columnHeaders.ToArray());//Устанавливаем заголовки для столбцов
            SetCellValues(worksheet, false, cellValues.ToArray());//Устанавливаем значения для ячеек средних цен/индексов и получаем след пустую строку
            if (name.Equals("AveragePrices"))
            {
                column = 2;
                SetHeaderRow(worksheet, "Используемые продукты для подсчета средних цен");//Устанавливаем заголовок дополнительной информации
                await SetRequiredProductPrices(worksheet, columnHeaders);
            }
            else
            {
                SetHeaderRow(worksheet, "Используемые средние цены для подсчета индексов средних цен");//Устанавливаем заголовок дополнительной информации
                await SetRequiredAverageProductPrices(worksheet, columnHeaders);
            }

            worksheet.Columns().AdjustToContents();
            workbook.SaveAs(filePathExcel);
            OpenFile(filePathExcel, excelPath);
        }
    }

    private static void SetFileTittle(IXLWorksheet worksheet, string title)//Установка заголовка файла (Оптимизировано)
    {
        for (int i = 0; i < title.Split("\n").Count(); i++)
        {
            SetHeaderRow(worksheet, "", true, title.Split("\n")[i].Trim());
        }
        row++;
    }
    private static void SetHeaderRow(IXLWorksheet worksheet, string requiredHeader = "", bool isTitle = false, params string[] headers)//Заполнение названий столбцов EXCEL(Optimized)
    {
        int columnCount = headers.Length > 0 ? headers.Length : 1;

        for (int i = 0; i < columnCount; i++)
        {
            var cell = worksheet.Cell(row, i + 1);

            if (headers.Length > 0)
            {
                cell.Value = headers[i];
            }
            else
            {
                cell = worksheet.Cell(row, column);
                cell.Value = requiredHeader;
            }
            cell.Style.Font.SetBold(true)
                          .Font.SetFontSize(16);
            if (!isTitle)
            {
                cell.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            }
        }
        ++row;
    }

    private static void SetCellValues(IXLWorksheet worksheet, bool areDates = false, params string[] cellValues)//Заполнение ячеек данными(Оптимизировано)
    {
        int cell = column;
        foreach (var cellValue in cellValues)
        {
            if (cellValue.Equals("end"))
            {
                row++; cell = 1;
                continue;
            }
            if (areDates)
            {
                worksheet.Cell(row, cell).Style.Font.SetFontSize(15);
            }
            worksheet.Cell(row, cell).Value = cellValue;
            worksheet.Cell(row, cell).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            cell++;
        }
        row++;
    }
    private static async Task SetRequiredProductPrices(IXLWorksheet worksheet, List<string> dates)//Дополнительная информация о продуктах для средних цен(Оптимизировано)
    {
        var allCategories = await SQLScripts.GetAllCategories();//Получаем все категории
        var allProducts = await SQLScripts.GetAllProducts();//Получаем все продукты
        var allProductPrices = await SQLScripts.GetAllPricesAsync();//Получаем все цены

        var requiredDates = GetRequiredDates(dates.TakeLast(dates.Count - 1).ToArray());//Получаем необходимые даты

        foreach (var category in allCategories)
        {
            SetHeaderRow(worksheet, category.Name);//Установка названия категории
            SetCellValues(worksheet, true, dates.TakeLast(dates.Count - 1).ToArray());
            int dataRow = row;
            var requiredProducts = allProducts.Where(x => x.CategoryId == category.Id);
            foreach (var date in requiredDates)
            {
                row = dataRow;
                var requiredPrices = allProductPrices.Where(x => x.PriceDate.Equals(date) &&
                                                    requiredProducts.Select(x => x.Article).Contains(x.ProductId)).Distinct().OrderBy(x => x.Price);//Получаем необходимые цены

                foreach (var price in requiredPrices)
                {
                    var productName = requiredProducts.FirstOrDefault(x => x.Article.Equals(price.ProductId))!.Name;
                    SetCellValues(worksheet, false, $"{productName}:\t\t{price.Price} бел. руб.");
                }
                column++;
            }
            row+=3;
            column = 2;
        }
    }
    private static async Task SetRequiredAverageProductPrices(IXLWorksheet worksheet, List<string> dates)
    {
        var allCategories = await SQLScripts.GetAllCategories();//Получаем все категории
        var allAveragePrices = await SQLScripts.GetAveragePricesAsync();//Получаем все средние цены

        List<DateTime> requiredDates = new List<DateTime>();

        foreach (var date in dates.TakeLast(dates.Count - 1))
        {
            foreach (var reqDate in GetRequiredDates(date.Replace("Период: ", "").Split('-')))
            {
                requiredDates.Add(reqDate);
            }
        }

        int dataRow = row;
        int counter = 1;
        foreach (var date in requiredDates)
        {
            row = dataRow;
            SetCellValues(worksheet, true, date.ToShortDateString());
            foreach (var category in allCategories)
            {
                var requiredAveragePrice = allAveragePrices.FirstOrDefault(x => x.CategoryId == category.Id && x.AveragePriceDate.Equals(date))!.Average_Price;
                SetCellValues(worksheet, false, $"{category.Name}: {requiredAveragePrice}");
            }
            counter++;
            column++;
        }
        row+=3;
        column = 1;
    }


    private static List<DateTime> GetRequiredDates(params string[] dates)//Получение всех дат, сипользуемых в отчете(Оптимизировано)
    {
        try
        {
            List<DateTime> result = new List<DateTime>();
            for (int i = 0; i < dates.Length; i++)
            {
                var date = dates[i].Trim();
                result.Add(DateTime.ParseExact(date, "dd MMMM yyyy", CultureInfo.CurrentCulture));
            }
            return result;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
        return new List<DateTime>() { DateTime.Now.Date };
    }

    public static void OpenFile(string filePath, string programmPath) //Открытие файла(Optimized)
    {
        try
        {
            Process.Start(programmPath, filePath);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
        }
    }

}
