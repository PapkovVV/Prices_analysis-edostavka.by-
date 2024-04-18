using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using KursovayaDB.DataBaseServices;
using KursovayaDB.Models;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace KursovayaDB.Services.ExportServices;

public class ExcelExport
{
    static int row = 1;//Строка
    static int column = 1;
    public static async Task ExcelImportAndOpenAsync(DataGrid dataGrid, string name, string title, bool isNewFile)
    {
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
            SetHeaderRow(worksheet, "", columnHeaders.ToArray());//Устанавливаем заголовки для столбцов
            SetCellValues(worksheet, cellValues.ToArray());//Устанавливаем значения для ячеек средних цен/индексов и получаем след пустую строку
            if (name.Equals("AveragePrices"))
            {
                SetHeaderRow(worksheet, "Используемые продукты для подсчета средних цен");//Устанавливаем заголовок дополнительной информации
                await SetRequiredProductPrices(worksheet, columnHeaders);
            }
            else
            {
                SetHeaderRow(worksheet, "Используемые средние цены для подсчета индексов средних цен");//Устанавливаем заголовок дополнительной информации
            }

            worksheet.Columns().AdjustToContents();
            workbook.SaveAs(filePathExcel);
            OpenFile(filePathExcel, excelPath);
        }
    }

    private static void SetFileTittle(IXLWorksheet worksheet,string title)//Установка заголовка файла (Оптимизировано)
    {
        for (int i = 0; i < title.Split("\n").Count(); i++)
        {
            SetHeaderRow(worksheet, "", title.Split("\n")[i].Trim());
        }
    }
    private static void SetHeaderRow(IXLWorksheet worksheet, string requiredHeader = "", params string[] headers)//Заполнение названий столбцов EXCEL(Optimized)
    {
        if (headers.Length > 0)
        {
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(row, i + 1).Value = headers[i];
                worksheet.Cell(row, i + 1).Style.Font.SetBold(true);
                worksheet.Cell(row, i + 1).Style.Font.SetFontSize(16);
            }
        }
        else
        {
            worksheet.Cell(row, 1).Value = requiredHeader;
            worksheet.Cell(row, 1).Style.Font.SetBold(true);
            worksheet.Cell(row, 1).Style.Font.SetFontSize(16);
        }
        ++row;
    }
    private static void SetCellValues(IXLWorksheet worksheet, params string[] cellValues)//Заполнение ячеек данными(Оптимизировано)
    {
        int cell = column;
        foreach (var cellValue in cellValues)
        {
            if (cellValue.Equals("end"))
            {
                row++; cell = 1;
                continue;
            }
            worksheet.Cell(row, cell).Value = cellValue;
            cell++;
        }
        row++;
    }
    private static async Task SetRequiredProductPrices(IXLWorksheet worksheet, List<string> dates)//Дополнительная информация о продуктах для средних цен(Оптимизировано)
    {
        var allCategories = await SQLScripts.GetAllCategories();//Получаем все категории
        var allProducts = await SQLScripts.GetAllProducts();//Получаем все продукты
        var allProductPrices = await SQLScripts.GetAllPricesAsync();//Получаем все цены

        var requiredDates = GetRequiredDates(dates);//Получаем необходимые даты

        foreach (var category in allCategories)
        {
            SetHeaderRow(worksheet, category.Name);//Установка названия категории
            SetCellValues(worksheet, dates.TakeLast(dates.Count - 1).ToArray());
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
                    SetCellValues(worksheet, $"{productName}:\t\t{price.Price} бел. руб.");
                }
                column++;
            }
            row+=3;
            column = 1;
        }
    }



    private static List<DateTime> GetRequiredDates(List<string> dates)//Получение всех дат, сипользуемых в отчете(Оптимизировано)
    {
        List<DateTime> result = new List<DateTime>();
        for (int i = 1; i < dates.Count; i++)
        {
            result.Add(DateTime.ParseExact(dates.ElementAt(i), "dd MMMM yyyy", CultureInfo.GetCultureInfo("ru-RU")));
        }
        return result;
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
