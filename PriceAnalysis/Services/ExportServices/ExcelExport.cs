using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;
using PriceAnalysis.DataBaseServices;
using System.Data;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace PriceAnalysis.Services.ExportServices;

public class ExcelExport : BaseExportClass
{
    static int row;//Строка
    static int column;//Столбец (ячейка)
    const string excelPath = @"C:\Program Files\Microsoft Office\root\Office16\EXCEL.EXE";
    const string sheetName = "Sheet1";
    static string timeline = "";

    public static async Task ExcelImportAndOpenAsync(DataGrid dataGrid, string name, string title, string timeLine, bool isNewFile)
    {
        timeline = timeLine;
        row = 1;
        column = 1;
        string filePathExcel = isNewFile ? $@"ExportedFiles\Excel\Exported{name}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx" :
            $@"ExportedFiles\Excel\Exported{name}.xlsx";

        //Основная информация
        List<string> columnHeaders = GetHeaderNames(dataGrid);//Получаем названия 
        List<string> cellValues = GetCellValues(dataGrid);//Получаем основные данные

        using (var workbook = new XLWorkbook())
        {
            var worksheet = workbook.Worksheets.Add(sheetName);

            SetFileTittle(worksheet, title);//Устанавливаем заголовок файла
            SetHeaderRow(worksheet, "", false, columnHeaders.ToArray());//Устанавливаем заголовки для столбцов
            SetCellValues(worksheet, false, cellValues.ToArray());//Устанавливаем значения для ячеек средних цен/индексов и получаем след пустую строку

            //Дополнительная информация
            if (name.Equals("AveragePrices"))
            {
                if (timeline.Equals("День"))
                {
                    column = 2;
                    SetHeaderRow(worksheet, "Используемые продукты для подсчета средних цен");//Устанавливаем заголовок дополнительной информации
                    await SetRequiredProductPrices(worksheet, columnHeaders);
                }
                else if(timeline.Equals("Месяц"))
                {
                    SetHeaderRow(worksheet, "Используемые ежедневные средние цены");//Устанавливаем заголовок дополнительной информации
                    await SetRequireAveragePricesPricesByMonth(worksheet, columnHeaders);
                }
            }
            else
            {
                SetHeaderRow(worksheet, "Используемые средние цены для подсчета индексов средних цен");//Устанавливаем заголовок дополнительной информации
                await SetRequiredAverageProductPrices(worksheet, columnHeaders);
            }

            worksheet.Columns().AdjustToContents();

            workbook.SaveAs(filePathExcel);

            SetBarChart(filePathExcel);//Создание столбиковой диаграммы

            try
            {
                workbook.SaveAs(filePathExcel);
                OpenFile(filePathExcel, excelPath);
            }
            catch (IOException)
            {
                MessageBox.Show("Перед перезаписью сперва закройте файл.");
            }
        }
    }

    #region Установка данных

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
            cell.Style.Font.SetBold(true).Font.SetFontSize(16);
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

            if (decimal.TryParse(cellValue, out decimal decimalValue))
            {
                worksheet.Cell(row, cell).Value = decimalValue;
            }
            else
            {
                worksheet.Cell(row, cell).Value = cellValue;
            }
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

        var requiredDates = GetRequiredDates(timeline, dates.TakeLast(dates.Count - 1).ToArray());//Получаем необходимые даты

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
    private static async Task SetRequiredAverageProductPrices(IXLWorksheet worksheet, List<string> dates)//Дополнительная информация о средних ценах продуктов для индексов
    {
        var allCategories = await SQLScripts.GetAllCategories();//Получаем все категории
        var allAveragePrices = await SQLScripts.GetAveragePricesAsync();//Получаем все средние цены

        List<DateTime> requiredDates = new List<DateTime>();

        foreach (var date in dates.TakeLast(dates.Count - 1))
        {
            foreach (var reqDate in GetRequiredDates(timeline, date.Replace("Период: ", "").Split('-')))
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
                var requiredAveragePrice = allAveragePrices.FirstOrDefault(x => x.CategoryId == category.Id && x.AveragePriceDate.Equals(date));

                if (requiredAveragePrice != null)
                {
                    SetCellValues(worksheet, false, $"{category.Name}: {requiredAveragePrice.Average_Price}");
                }
            }
            counter++;
            column++;
        }
        row+=3;
        column = 1;
    }
    private static async Task SetRequireAveragePricesPricesByMonth(IXLWorksheet worksheet, List<string> dates)
    {
        var allCategories = await SQLScripts.GetAllCategories();//Получаем все категории
        var allAveragePrices = await SQLScripts.GetAveragePricesAsync();//Получаем все средние цены

        List<DateTime> requiredDates = new List<DateTime>();

        foreach (var date in dates.TakeLast(dates.Count - 1))
        {
            foreach (var reqDate in GetRequiredDates(timeline, date))
            {
                requiredDates.Add(reqDate);
            }
        }

        int dataRow = 0;
        foreach (var category in allCategories)
        {
            row = GetEmptyRow(worksheet) + 1;
            SetCellValues(worksheet, true, category.Name);
            dataRow = row;
            foreach (var date in requiredDates)
            {
                row = dataRow;
                SetCellValues(worksheet, true, date.ToString("MMMM yyyy"));
                var requiredAveragePrices = allAveragePrices.Where(x => x.CategoryId == category.Id &&
                                                                            x.AveragePriceDate.Month.Equals(date.Month) &&
                                                                            x.AveragePriceDate.Year.Equals(date.Year));
                foreach (var price in requiredAveragePrices)
                {
                    SetCellValues(worksheet, false, $"{price.AveragePriceDate.ToShortDateString()}: {price.Average_Price}");
                }
                column++;
            }
            column = 1;
        }
        row+=3;
        column = 1;
    }

    private static void SetBarChart(string filePath)//Создание столбиковой диаграммы на основе необходимых данных
    {
        var excelFile = new FileInfo(filePath);

        using (var package = new ExcelPackage(excelFile))
        {
            // Устанавливаем контекст лицензии
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            var worksheet = package.Workbook.Worksheets["Sheet1"] ?? package.Workbook.Worksheets.Add("Sheet1");
            // Проход по всем столбцам справа от начального столбца

            int rowNum = 5; // Рядок для поиска
            int startColumn = 2; // B это второй столбец
            int lastNonEmptyColumn = GetEmptyCell(worksheet, startColumn, rowNum) - 1;

            if (lastNonEmptyColumn != -1)
            {
                string lastNonEmptyCellAddress = worksheet.Cells[rowNum, lastNonEmptyColumn].Address;
                string cellValue = worksheet.Cells[$"A{++rowNum}"].Text;
                int emptyCell = GetEmptyCell(worksheet, startColumn, 5);
                int emptyRow = 0;

                while (!string.IsNullOrEmpty(cellValue))
                {
                    var chart = worksheet.Drawings.AddChart($"{cellValue}Chart", eChartType.ColumnClustered) as ExcelBarChart;
                    chart.Title.Text = cellValue;
                    chart.SetPosition(emptyRow, 0, emptyCell, 0); // Позиция диаграммы (row, rowOffsetPixels, column, columnOffsetPixels)
                    chart.SetSize(800, 600); // Размер диаграммы

                    var series = chart.Series.Add(worksheet.Cells[$"B{rowNum}:{RemoveNonLetters(lastNonEmptyCellAddress)}{rowNum}"], worksheet.Cells[$"B5:{lastNonEmptyCellAddress}"]);
                    series.Header = "Prices";

                    cellValue = worksheet.Cells[$"A{++rowNum}"].Text;

                    if (emptyRow == 0)
                    {
                        emptyRow = 38;
                    }
                    else
                    {
                        emptyCell += 15;
                        emptyRow -= 38;
                    }
                }

                package.Save();

            }
            else
            {
                MessageBox.Show($"В строке {rowNum} нет непустых ячеек начиная с B5");
            }
        }
    }

    #endregion

    #region Получение данных

    private static List<string> GetHeaderNames(DataGrid dataGrid)//Получаем названия столбцов(Оптимизировано)
    {
        return dataGrid.Columns.OfType<DataGridTextColumn>()
                               .Select(column => column.Header?.ToString()!)
                               .ToList();
    }
    private static List<string> GetCellValues(DataGrid dataGrid)
    {
        List<string> cellValues = new List<string>();

        foreach (var item in dataGrid.Items)
        {
            if (item is DataRowView row)
            {
                foreach (var cellValue in row.Row.ItemArray)
                {
                    cellValues.Add(cellValue!.ToString()!);
                }
                cellValues.Add("end");
            }
        }

        return cellValues;
    }
    private static int GetEmptyRow(IXLWorksheet worksheet)//Нахождение первой пустой строки после последней непустой(Оптимизировано)
    {
        int lastRow = worksheet.LastRowUsed().RowNumber();
        int emptyRow = lastRow + 1;
        
        while (!string.IsNullOrEmpty(worksheet.Cell(emptyRow, 1).Value.ToString()))
        {
            emptyRow++;
        }

        return emptyRow;
    }
    private static int GetEmptyCell(ExcelWorksheet worksheet, int startColumn, int rowNum)//Получение первой пустой ячейки после последней непустой(Оптимизировано)
    {
        int lastNonEmptyColumn = 0;

        for (int col = startColumn; col <= worksheet.Dimension.End.Column; col++)
        {
            if (!string.IsNullOrEmpty(worksheet.Cells[rowNum, col].Text))
            {
                lastNonEmptyColumn = col;
            }
        }

        return lastNonEmptyColumn + 1;
    }

    #endregion

    private static string RemoveNonLetters(string input)
    {
        return Regex.Replace(input, "[^a-zA-Zа-яА-Я]", "");
    }
}
