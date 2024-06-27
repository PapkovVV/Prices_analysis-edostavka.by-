using Nethereum.Util;
using PriceAnalysis.DataBaseServices;
using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Xceed.Words.NET;
using Alignment = Xceed.Document.NET.Alignment;

namespace PriceAnalysis.Services.ExportServices;

public class WordExport : BaseExportClass
{
    static string timeline = "";
    public static async Task WordImportAndOpen(DataGrid dataGrid, string name, string title, string timeLine, bool isNewFile)
    {
        string wordPath = @"C:\Program Files\Microsoft Office\root\Office16\WINWORD.EXE"; // Путь к исполняемому файлу Microsoft Word
        string filePathWord = isNewFile ? $@"ExportedFiles\Word\Exported{name}_{DateTime.Now:yyyyMMdd_HHmmss}.docx" :
            $@"ExportedFiles\Word\Exported{name}.docx";
        timeline = timeLine;

        List<string> columnHeaders = new List<string>();//Названия столбцов
        foreach (var column in dataGrid.Columns)
        {
            if (column is DataGridTextColumn textColumn)
            {
                columnHeaders.Add(textColumn.Header.ToString()!);
            }
        }

        List<string> cellValues = new List<string>();//Названия столбцов

        foreach (var item in dataGrid.Items)
        {
            if (item is DataRowView row)
            {
                foreach (var cellValue in row.Row.ItemArray)
                {
                    cellValues.Add(cellValue.ToString()!);
                }
                cellValues.Add("end");
            }
        }

        List<string> selectedCategories = new List<string>();//Получаем выбранные категории

        foreach (string cellValue in cellValues)
        {
            if (cellValue.Any(char.IsLetter) && !cellValue.Equals("end"))
            {
                selectedCategories.Add(cellValue);
            }
        }

        using (var doc = DocX.Create(filePathWord))
        {
            CreateDataTable(doc, filePathWord, dataGrid, columnHeaders, title, cellValues);
            if (timeline.Equals("День"))
            {
                if (name.Equals("AveragePrices"))
                {
                    await SetRequiredProductPricesAsync(doc, columnHeaders, "Информация о продуктах, используемых для расчета средних цен", selectedCategories);
                }
                else
                {
                    await SetRequiredAverageProductPrices(doc, columnHeaders, "Информация о средних ценах, используемых для расчета индексов потребительских цен");
                }
            }
            else if (timeline.Equals("Месяц"))
            {
                if (name.Equals("AveragePrices"))
                {
                    //await SetRequiredAveragePricesByMonth(doc, columnHeaders, "Информация о продуктах, используемых для расчета средних цен", selectedCategories);
                }
                else
                {
                    await SetRequiredAveragePricesByMonth(doc, columnHeaders, "Информация о средних ценах, используемых для расчета индексов потребительских цен", selectedCategories);
                }
            }


            try
            {
                doc.Save();
                OpenFile(filePathWord, wordPath);
            }
            catch (IOException)
            {
                MessageBox.Show("Перед перезаписью сперва закройте файл.");
            }
        }
    }

    private static void CreateDataTable(DocX doc, string filePathWord, DataGrid dataGrid, List<string> columnHeaders, string title, List<string> cellValues)//Создание таблицы основной информации
    {
        doc.InsertParagraph(title).FontSize(14).Font("Times New Roman").Alignment = Alignment.center;
        doc.InsertParagraph();

        var table = doc.AddTable(dataGrid.Items.Count+1, columnHeaders.Count);
        for (int i = 0; i < columnHeaders.Count; i++)
        {
            table.Rows[0].Cells[i].Paragraphs.First().Append(columnHeaders.ElementAt(i)).Bold().FontSize(9).Font("Times New Roman").Alignment = Alignment.center;
        }

        int row = 1;
        int cell = 0;
        foreach (var cellValue in cellValues)
        {
            if (cellValue.Equals("end"))
            {
                row++; cell = 0;
                continue;
            }
            table.Rows[row].Cells[cell].Paragraphs.First().Append(cellValue).FontSize(9).Font("Times New Roman").Alignment = Alignment.center;
            cell++;
        }

        doc.InsertTable(table);
    }

    //Создание таблицы дополнительной информации средних цен
    private static async Task SetRequiredProductPricesAsync(DocX doc, List<string> dates, string title, List<string> selectedCategories)
    {
        var allCategories = await SQLScripts.GetAllCategories();//Получаем все категории
        var allProducts = await SQLScripts.GetAllProducts();//Получаем все продукты
        var allProductPrices = await SQLScripts.GetAllPricesAsync();//Получаем все цены

        var requiredDates = GetRequiredDates(timeline, dates.TakeLast(dates.Count - 1).ToArray());//Получаем необходимые даты

        doc.InsertParagraph().SpacingAfter(10); // Вставляем пустую строку
        doc.InsertParagraph(title).FontSize(13).Font("Times New Roman").Alignment = Alignment.center;
        doc.InsertParagraph().SpacingAfter(10); // Вставляем пустую строку

        foreach (var category in allCategories.Where(category => selectedCategories.Contains(category.Name)))
        {
            doc.InsertParagraph(category.Name).FontSize(13).Bold().Font("Times New Roman").Alignment = Alignment.center;
            var requiredProducts = allProducts.Where(x => x.CategoryId == category.Id);
            var table = doc.AddTable(1, 3);

            table.Rows[0].Cells[0].Paragraphs.First().Append("Дата").Bold().FontSize(9).Font("Times New Roman").Alignment = Alignment.center;
            table.Rows[0].Cells[1].Paragraphs.First().Append("Продукт").Bold().FontSize(9).Font("Times New Roman").Alignment = Alignment.center;
            table.Rows[0].Cells[2].Paragraphs.First().Append("Цена").Bold().FontSize(9).Font("Times New Roman").Alignment = Alignment.center;

            foreach (var date in requiredDates)
            {
                var requiredPrices = allProductPrices.Where(x => x.PriceDate.Equals(date) &&
                                requiredProducts.Select(x => x.Article).Contains(x.ProductId)).Distinct().OrderBy(x => x.Price);//Получаем необходимые цены

                foreach (var price in requiredPrices)
                {
                    var newRow = table.InsertRow();

                    DateTime priceDate = date;//Дата
                    string productName = requiredProducts.FirstOrDefault(x => x.Article.Equals(price.ProductId))!.Name;//Продукт
                    decimal productPrice = price.Price;//Цена

                    newRow.Cells[0].Paragraphs.First().Append(date.ToShortDateString()).FontSize(9).Font("Times New Roman").Alignment = Alignment.center;
                    newRow.Cells[1].Paragraphs.First().Append(productName).FontSize(9).Font("Times New Roman").Alignment = Alignment.center;
                    newRow.Cells[2].Paragraphs.First().Append(productPrice.ToString("0.00")).FontSize(9).Font("Times New Roman").Alignment = Alignment.center;
                }
            }
            doc.InsertTable(table);
            doc.InsertParagraph();
            doc.InsertParagraph();
        }
    }

    //Создание таблицы дополнительной информации индексов потребительских цен
    private static async Task SetRequiredAverageProductPrices(DocX doc, List<string> dates, string title)
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

        doc.InsertParagraph().SpacingAfter(10); // Вставляем пустую строку
        doc.InsertParagraph(title).FontSize(13).Font("Times New Roman").Alignment = Alignment.center;
        doc.InsertParagraph().SpacingAfter(10); // Вставляем пустую строку


        foreach (var date in requiredDates)
        {
            var table = doc.AddTable(1, 2);
            table.Rows[0].MergeCells(0, 1);
            table.Rows[0].Cells[0].Paragraphs.First().Append(date.ToShortDateString()).Font("Times New Roman").FontSize(9).Bold().Alignment = Alignment.center;

            foreach (var category in allCategories)
            {
                var newRow = table.InsertRow();

                string requiredCategory = category.Name.ToString();//Категория
                decimal requiredAveragePrice = allAveragePrices.
                                               FirstOrDefault(x => x.CategoryId == category.Id && x.AveragePriceDate.Equals(date))!.Average_Price;//Средняя цена

                newRow.Cells[0].Paragraphs.First().Append(requiredCategory).FontSize(9).Font("Times New Roman").Alignment = Alignment.center;
                newRow.Cells[1].Paragraphs.First().Append(requiredAveragePrice.ToString("0.00")).FontSize(9).Font("Times New Roman").Alignment = Alignment.center;
            }
            doc.InsertTable(table);
            doc.InsertParagraph();
            doc.InsertParagraph();
        }
    }

    private static async Task SetRequiredAveragePricesByMonth(DocX doc, List<string> dates, string title, List<string> selectedCategories)
    {
        var allCategories = (await SQLScripts.GetAllCategories()).Where(category => selectedCategories.Contains(category.Name)).ToList();//Получаем все категории
        var allAveragePrices = await SQLScripts.GetAveragePricesAsync();//Получаем все средние цены

        HashSet<DateTime> requiredDates = new HashSet<DateTime>();

        foreach (var date in dates.TakeLast(dates.Count - 1))
        {
            List<DateTime> reqDates = new List<DateTime>();

            foreach (var reqDate in GetRequiredDates(timeline, date.Replace("Период: ", "").Split('-')))
            {
                requiredDates.Add(reqDate);
            }
        }

        doc.InsertParagraph().SpacingAfter(10); // Вставляем пустую строку
        doc.InsertParagraph(title).FontSize(13).Font("Times New Roman").Alignment = Alignment.center;
        doc.InsertParagraph().SpacingAfter(10); // Вставляем пустую строку

        foreach (var date in requiredDates)
        {
            var table = doc.AddTable(1, 2);
            table.Rows[0].MergeCells(0, 1);
            table.Rows[0].Cells[0].Paragraphs.First().Append(date.ToString("MMMM yyyy")).Font("Times New Roman").FontSize(9).Bold().Alignment = Alignment.center;

            foreach (var category in allCategories)
            {
                var newRow = table.InsertRow();

                string requiredCategory = category.Name.ToString();//Категория
                var averagePriceofAveragePrices = allAveragePrices.Where(x => x.CategoryId == category.Id
                                                && x.AveragePriceDate.Month == date.Month && x.AveragePriceDate.Year == date.Year)
                                                    .Select(x => x.Average_Price).ToList();//Получаем все цены за определенный месяц определенной каегории

                BigDecimal multipliedValues = averagePriceofAveragePrices
                                .Select(price => new BigDecimal(price))
                                .Aggregate(new BigDecimal(1), (acc, price) => acc * price);


                var requiredAveragePrice = Math.Pow((double)multipliedValues, 1.0 / averagePriceofAveragePrices.Count);//Средняя цена

                newRow.Cells[0].Paragraphs.First().Append(requiredCategory).FontSize(9).Font("Times New Roman").Alignment = Alignment.center;
                newRow.Cells[1].Paragraphs.First().Append(requiredAveragePrice.ToString("0.00")).FontSize(9).Font("Times New Roman").Alignment = Alignment.center;
            }
            doc.InsertTable(table);
            doc.InsertParagraph();
            doc.InsertParagraph();
        }
    }
}
