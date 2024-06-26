﻿using FullControls.Controls;
using Nethereum.Util;
using PriceAnalysis.DataBaseServices;
using PriceAnalysis.Models;
using PriceAnalysis.ViewModel;
using System.Data;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace PriceAnalysis.Views.Pages;

public partial class DataPage : Page
{
    static DataTable dataTable = new DataTable();

    static DateTime? lastDate;
    static DateTime? startDate;

    static decimal? lastPrice = null;
    static decimal? startPrice = null;


    string parameter = string.Empty;
    public DataPage(string parameter)
    {
        InitializeComponent();
        this.parameter = parameter;
        Initialize();
        DataContext = new DataPageViewModel(parameter) { DataGrid = dataGrid };
    }

    async void Initialize()
    {
        lastPrice = null;
        startPrice = null;
        await GenerateCategoriesComboBox();
        await GenerateTimeLinesComboBox();

        if (parameter.Equals("Цены")) GenerateAveragePricesDataGrid();
        else GeneratePriceIndexesDataGrid();

    }

    #region Генерация ComboBox
    //Генерация комбобокса с временными разрезами(Оптимизировано)
    private async Task GenerateTimeLinesComboBox()
    {
        string[] timeLines = { "День", "Месяц", "Год" };//Промежутки расчетов

        foreach (var timeLine in timeLines)
        {
            timelineCombo.Items.Add(new ComboBoxItemPlus()
            {
                Content = timeLine,
                HorizontalContentAlignment = HorizontalAlignment.Center,
            });
        }
    }

    //Генерация комбобокса с категориями(Оптимизировано)
    private async Task GenerateCategoriesComboBox()
    {
        var uniqueCategories = await SQLScripts.GetAllCategories();
        foreach (var category in uniqueCategories.Select(x => x.Name))
        {
            categoriesCombo.Items.Add(new ComboBoxItemPlus
            {
                Content = category
            });
        }
    }

    #endregion Генерация ComboBox

    #region Генерация DataGrid

    async void GeneratePriceIndexesDataGrid()//Генерация Индексов DataGrid
    {
        var priceIndexes = await SQLScripts.GetAllPriceIndexes();

        List<string> uniqueCategories = await GetNeededCategories();
        priceIndexes = await GetNeededObjects(priceIndexes);//Отображение данных в соответствии с поиском

        var distinctPriceIndexes = priceIndexes.Where(x => x.IndexDateFrom >= startDate && x.IndexDateTo <= lastDate)
                                    .GroupBy(pi => new { pi.IndexDateFrom, pi.IndexDateTo })
                                    .Select(group => group.First())
                                    .TakeLast(7).ToList();


        if (distinctPriceIndexes != null && distinctPriceIndexes.Count > 0)
        {
            dataTable = new DataTable();

            dataTable.Columns.Add("Категория", typeof(string));
            foreach (var priceInd in distinctPriceIndexes)
            {
                var header = GetHeaderName(priceInd.IndexDateFrom, priceInd.IndexDateTo);
                dataTable.Columns.Add(header, typeof(decimal));
            }

            foreach (var category in uniqueCategories)
            {
                dataTable.Rows.Add(dataTable.NewRow()[0] = category);
            }

            foreach (var priceInd in distinctPriceIndexes)
            {
                var header = GetHeaderName(priceInd.IndexDateFrom, priceInd.IndexDateTo);

                for (int i = 0; i < uniqueCategories.Count; i++)
                {
                    PriceIndex? u = priceIndexes.FirstOrDefault(x => x.CategoryName.Equals(uniqueCategories.ElementAt(i)) &&
                        x.IndexDateFrom == priceInd.IndexDateFrom && x.IndexDateTo == priceInd.IndexDateTo);

                    dataTable.Rows[i][header] = u?.IndexValue != null ? u.IndexValue.ToString("0.00") : (object)DBNull.Value;
                }
            }

            DataTable filteredDataTable = dataTable.Clone();

            // Пройдите по каждой строке в исходной таблице
            foreach (DataRow row in dataTable.Rows)
            {
                // Проверьте, есть ли хотя бы одна пустая ячейка в текущей строке
                bool hasEmptyCell = false;
                foreach (var item in row.ItemArray)
                {
                    if (item == DBNull.Value || string.IsNullOrEmpty(item.ToString()))
                    {
                        hasEmptyCell = true;
                        break;
                    }
                }

                // Если все ячейки в текущей строке заполнены, добавьте эту строку в отфильтрованную таблицу
                if (!hasEmptyCell)
                {
                    filteredDataTable.Rows.Add(row.ItemArray);
                }
            }

            dataTable = filteredDataTable;

        }
        else
        {
            dataGrid.Columns.Clear();
        }
        SeTDataGrid();
    }
    async void GenerateAveragePricesDataGrid()//Генерация средних цен DataGrid(Оптимизировано)
    {
        var averagePrices = await SQLScripts.GetAveragePricesAsync();

        List<string> uniqueCategories = await GetNeededCategories(); //Категории для отображения в DataGrid

        averagePrices = await GetNeededObjects(averagePrices);//Отображение данных в соответствии с поиском

        List<DateTime> uniqueDates = AveragePricesPriceFilter(averagePrices);//Список дат в соответствии с ценовым фильтром

        GenerateDataTable(uniqueCategories, averagePrices, uniqueDates);//Генерация основы для отображения данных
        SeTDataGrid();// Генерация таблицы для отображения данных
    }

    #endregion Генерация DataGrid

    #region Все для генерации DataGrid с необходимыми данными

    private async Task<List<string>> GetNeededCategories()//Получение необходимых категорий для отображения (Оптимизировано)
    {
        var allCategories = await SQLScripts.GetAllCategories();

        if (!string.IsNullOrEmpty(categoriesCombo.Text))
        {
            return allCategories.Select(x => x.Name).
                Where(x => x.ToLower().StartsWith(categoriesCombo.Text.ToLower())).OrderBy(x => x).Distinct().ToList();
        }
        return allCategories.Select(x => x.Name).OrderBy(x => x).Distinct().ToList();
    }
    private async Task<List<T>> GetNeededObjects<T>(List<T> dataList)//Получение списка объектов в соответствии с поиском (Оптимизировано)
    {
        List<T> result = new List<T>();//Список результата

        if (typeof(T) == typeof(AveragePrice))
        {
            List<AveragePrice> averagePrices = dataList.Cast<AveragePrice>().ToList();//Объявляем список средних цен

            if (GetTimeLine().Equals("Месяц"))
            {
                averagePrices = GetMonthlyAveragePrices(averagePrices);
            }
            else if (GetTimeLine().Equals("Год"))
            {
                averagePrices = GetAveragePricesByYear(averagePrices);
            }

            if (averagePrices.Any(x => x.CategoryName.ToLower().StartsWith(categoriesCombo.Text.ToLower())))
            {
                result = averagePrices.Where(x => x.CategoryName.ToLower().StartsWith(categoriesCombo.Text.ToLower())).Cast<T>().ToList();
            }
        }
        else
        {
            List<PriceIndex> priceIndexes = new List<PriceIndex>();

            if (GetTimeLine().Equals("День"))
            {
                priceIndexes = dataList.Cast<PriceIndex>().ToList();
            }
            else
            {
                var allAveragePrices = await SQLScripts.GetAveragePricesAsync();

                if (GetTimeLine().Equals("Месяц"))
                {
                    priceIndexes = GetMonthlyPriceIndexes(allAveragePrices);
                }
                else
                {
                    int yearsCount = allAveragePrices.Select(x => x.AveragePriceDate.Year).Distinct().Count();

                    if (yearsCount >= 2)
                    {
                        priceIndexes = GetPriceIndexesByYear(allAveragePrices);
                    }
                    else
                    {
                        MessageBox.Show($"Данных пока нет!");
                        SetTimeLine("День");
                    }
                }
            }

            if (priceIndexes.Any(x => x.CategoryName.ToLower().StartsWith(categoriesCombo.Text.ToLower())))
            {
                result = priceIndexes.Where(x => x.CategoryName.ToLower().StartsWith(categoriesCombo.Text.ToLower())).Cast<T>().ToList();
            }
        }

        return result;
    }
    private void GenerateDataTable(List<string> uniqueCategories, List<AveragePrice> averagePrices, List<DateTime> uniqueDates)//Генерация основы данных для DataGrid
    {
        dataTable = new DataTable();
        if (uniqueDates != null && uniqueDates.Count > 0)
        {
            dataTable.Columns.Add("Категория", typeof(string));

            foreach (var date in uniqueDates)
            {
                string header = GetHeaderName(date);
                if (!dataTable.Columns.Contains(header))
                {
                    dataTable.Columns.Add(header, typeof(decimal));
                }
            }

            foreach (var category in uniqueCategories)
            {
                dataTable.Rows.Add(dataTable.NewRow()[0] = category);
            }

            foreach (var date in uniqueDates)
            {
                string header = GetHeaderName(date);
                for (int i = 0; i < uniqueCategories.Count; i++)
                {
                    var categoryName = uniqueCategories.ElementAt(i);
                    var price = averagePrices.FirstOrDefault(x =>
                        x.CategoryName.Equals(categoryName) &&
                        x.AveragePriceDate == date &&
                        (startPrice == null || x.Average_Price >= startPrice) &&
                        (lastPrice == null || x.Average_Price <= lastPrice));

                    if (price != null)
                    {
                        dataTable.Rows[i][header] = price.Average_Price.ToString("0.00");
                    }
                }
            }

            DataTable filteredDataTable = dataTable.Clone();

            foreach (DataRow row in dataTable.Rows)
            {
                bool hasEmptyCell = false;
                foreach (var item in row.ItemArray)
                {
                    if (item == DBNull.Value || string.IsNullOrEmpty(item.ToString()))
                    {
                        hasEmptyCell = true;
                        break;
                    }
                }

                if (!hasEmptyCell)
                {
                    filteredDataTable.Rows.Add(row.ItemArray);
                }
            }

            dataTable = filteredDataTable;
        }
        else
        {
            dataGrid.Columns.Clear();
        }
    }
    void SeTDataGrid()//Генерация таблицы для отображения необходимых данных(Оптимизировано)
    {
        dataGrid.ItemsSource = dataTable.DefaultView;
        foreach (var column in dataGrid.Columns)
        {
            column.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            column.MinWidth = 150;
        }

        for (int i = 1; i < dataGrid.Columns.Count; i++)
        {
            var column = dataGrid.Columns[i];
            if (column is DataGridTextColumn textColumn)
            {
                textColumn.ElementStyle = new Style(typeof(TextBlock))
                {
                    Setters = { new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right) }
                };
                column.HeaderStyle = new Style(typeof(DataGridColumnHeader))
                {
                    Setters = { new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Right) }
                };
            }
        }

    }

    #endregion Все для генерации DataGrid с необходимыми данными

    #region Фильтры

    private List<DateTime> AveragePricesPriceFilter(List<AveragePrice> averagePrices)//Фильтр данных по необходимым средним ценам (Оптимизировано)
    {
        IEnumerable<AveragePrice>? query = new List<AveragePrice>();

        if (GetTimeLine().Equals("День"))
        {
            query = averagePrices.Where(x => x.AveragePriceDate >= startDate && x.AveragePriceDate <= lastDate);
        }
        else //if(GetTimeLine().Equals("Месяц"))
        {
            query = averagePrices.Where(x => x.AveragePriceDate.Month >= startDate?.Month && x.AveragePriceDate.Month <= lastDate?.Month);
        }

        if (startPrice != null)
        {
            query = query.Where(x => x.Average_Price >= startPrice);
        }

        if (lastPrice != null)
        {
            query = query.Where(x => x.Average_Price <= lastPrice);
        }

        return query
            .Select(x => x.AveragePriceDate)
            .OrderBy(x => x)
            .Distinct()
            .TakeLast(7)
            .ToList();
    }
    private List<AveragePrice> GetMonthlyAveragePrices(List<AveragePrice> allAveragePrices)//Получение списка средних цен в разрезе месяца
    {
        List<AveragePrice> averagePricesByMonth = new List<AveragePrice>();

        var requiredCategoryIds = allAveragePrices.Select(x => new { CategoryId = x.CategoryId, CategoryName = x.CategoryName }).Distinct();//Получаем все Id имеющихся категорий
        var allMonthsAndYears = allAveragePrices.Select(x => new { Month = x.AveragePriceDate.Month, Year = x.AveragePriceDate.Year }).Distinct();

        foreach (var categoryItem in requiredCategoryIds)
        {
            foreach (var dateItem in allMonthsAndYears)
            {
                var averagePriceofAveragePrices = allAveragePrices.Where(x => x.CategoryId == categoryItem.CategoryId
                                                && x.AveragePriceDate.Month == dateItem.Month && x.AveragePriceDate.Year == dateItem.Year)
                                                    .Select(x => x.Average_Price).ToList();//Получаем все цены за определенный месяц определенной каегории

                BigDecimal multipliedValues = averagePriceofAveragePrices
                                .Select(price => new BigDecimal(price))
                                .Aggregate(new BigDecimal(1), (acc, price) => acc * price);

                var averagePriceOfMonth = Math.Pow((double)multipliedValues, 1.0 / averagePriceofAveragePrices.Count);

                averagePricesByMonth.Add(new AveragePrice
                {
                    CategoryId = categoryItem.CategoryId,
                    AveragePriceDate = new DateTime(dateItem.Year, dateItem.Month, allAveragePrices.Where(x => x.AveragePriceDate.Month == dateItem.Month && x.AveragePriceDate.Year == dateItem.Year).
                                                     Select(x => x.AveragePriceDate).Distinct().Min().Day),
                    CategoryName = categoryItem.CategoryName,
                    Average_Price = (decimal)averagePriceOfMonth
                });
            }
        }

        return averagePricesByMonth;
    }
    private List<PriceIndex> GetMonthlyPriceIndexes(List<AveragePrice> allAveragePrices)//Получение списка индексов цен в разрезе месяца
    {
        List<PriceIndex> priceIndexesByMonth = new List<PriceIndex>();

        var requiredAveragePricesByMonth = GetMonthlyAveragePrices(allAveragePrices);

        var requiredCategoryIds = allAveragePrices.Select(x => new { CategoryId = x.CategoryId, CategoryName = x.CategoryName }).Distinct();//Получаем все Id и названия имеющихся категорий

        foreach (var categoryItem in requiredCategoryIds)
        {
            var requiredCategoryPrices = requiredAveragePricesByMonth.Where(x => x.CategoryId == categoryItem.CategoryId).OrderBy(x => x.AveragePriceDate);
            for (int i = 0; i < requiredCategoryPrices.Count() - 1; i++)
            {
                priceIndexesByMonth.Add(new PriceIndex
                {
                    CategoryId = categoryItem.CategoryId,
                    CategoryName = categoryItem.CategoryName,
                    IndexDateFrom = requiredCategoryPrices.ElementAt(i).AveragePriceDate,
                    IndexDateTo = requiredCategoryPrices.ElementAt(i + 1).AveragePriceDate,
                    IndexValue = (requiredCategoryPrices.ElementAt(i + 1).Average_Price/requiredCategoryPrices.ElementAt(i).Average_Price) * 100
                });
            }
        }

        return priceIndexesByMonth;
    }
    private List<AveragePrice> GetAveragePricesByYear(List<AveragePrice> allAveragePrices)//Получение списка средних цен в разрезе года
    {
        List<AveragePrice> averagePricesByYear = new List<AveragePrice>();

        var requiredCategoryIds = allAveragePrices.Select(x => new { CategoryId = x.CategoryId, CategoryName = x.CategoryName }).Distinct();//Получаем все Id имеющихся категорий
        var allMonthsAndYears = allAveragePrices.Select(x => x.AveragePriceDate.Year ).Distinct();

        foreach (var categoryItem in requiredCategoryIds)
        {
            foreach (var dateItem in allMonthsAndYears)
            {
                var averagePriceofAveragePrices = allAveragePrices.Where(x => x.CategoryId == categoryItem.CategoryId
                                                                && x.AveragePriceDate.Year == dateItem)
                                                                    .Select(x => x.Average_Price).ToList();//Получаем все цены за определенный месяц определенной каегории

                var multipliedValues = averagePriceofAveragePrices.Aggregate(1.0, (acc, price) => acc * (double)price);
                var averagePriceOfYear = Math.Pow((double)multipliedValues, 1.0 / averagePriceofAveragePrices.Count);

                averagePricesByYear.Add(new AveragePrice
                {
                    CategoryId = categoryItem.CategoryId,
                    AveragePriceDate = new DateTime(dateItem, allAveragePrices.Where(x => x.AveragePriceDate.Year == dateItem).
                                                     Select(x => x.AveragePriceDate).Distinct().Min().Month, allAveragePrices.Where(x => x.AveragePriceDate.Year == dateItem).
                                                     Select(x => x.AveragePriceDate).Distinct().Min().Day),
                    CategoryName = categoryItem.CategoryName,
                    Average_Price = (decimal)averagePriceOfYear
                });
            }
        }

        return averagePricesByYear;
    }
    private List<PriceIndex> GetPriceIndexesByYear(List<AveragePrice> allAveragePrices)//Получение списка индексов потребительских цен в разрезе года
    {
        List<PriceIndex> priceIndexesByYear = new List<PriceIndex>();

        var requiredAveragePricesByYear = GetAveragePricesByYear(allAveragePrices);

        var requiredCategoryIds = allAveragePrices.Select(x => new { CategoryId = x.CategoryId, CategoryName = x.CategoryName }).Distinct();//Получаем все Id и названия имеющихся категорий

        foreach (var categoryItem in requiredCategoryIds)
        {
            var requiredCategoryPrices = requiredAveragePricesByYear.Where(x => x.CategoryId == categoryItem.CategoryId).OrderBy(x => x.AveragePriceDate);
            for (int i = 0; i < requiredCategoryPrices.Count() - 1; i++)
            {
                priceIndexesByYear.Add(new PriceIndex
                {
                    CategoryId = categoryItem.CategoryId,
                    CategoryName = categoryItem.CategoryName,
                    IndexDateFrom = requiredCategoryPrices.ElementAt(i).AveragePriceDate,
                    IndexDateTo = requiredCategoryPrices.ElementAt(i + 1).AveragePriceDate,
                    IndexValue = (requiredCategoryPrices.ElementAt(i + 1).Average_Price/requiredCategoryPrices.ElementAt(i).Average_Price) * 100
                });
            }
        }

        return priceIndexesByYear;
    }
    #endregion Фильтры

    #region Работа со строковыми данными

    static string RemoveNonNumeric(string input)
    {
        string result = Regex.Replace(input, @"[^\d.]", "");
        return result;
    }
    private string GetHeaderName(DateTime firstDate, DateTime? secondDate = null)//Получение названия столбца для DataGrid (Оптимизировано)
    {
        string header = string.Empty;
        if (secondDate != null)
        {
            var dateFrom = RemoveNonNumeric(firstDate.ToShortDateString()).Split(".");
            var dateTo = RemoveNonNumeric(secondDate?.ToShortDateString()!).Split(".");

            if (GetTimeLine().Equals("День"))
            {
                header = $"Период: {dateFrom[0]} {GetMonth(dateFrom[1])} {dateFrom[2]} -\n" +
                        $"{dateTo[0]} {GetMonth(dateTo[1])} {dateTo[2]}";
            }
            else if (GetTimeLine().Equals("Месяц"))
            {
                header = $"Период: {GetMonth(dateFrom[1])} {dateFrom[2]} - {GetMonth(dateTo[1])} {dateTo[2]}";
            }
            else
            {
                header = $"Период: {dateFrom[2]} - {dateTo[2]}";
            }
        }
        else
        {
            string[] dateMass = RemoveNonNumeric(firstDate.ToShortDateString()).Split(".");
            if (GetTimeLine().Equals("День"))
            {
                header = $"{dateMass[0]} {GetMonth(dateMass[1])} {dateMass[2]}";
            }
            else if(GetTimeLine().Equals("Месяц"))
            {
                header = $"{GetMonth(dateMass[1])} {dateMass[2]}";
            }
            else
            {
                header = $"{dateMass[2]}";
            }
        }
        return header;
    }
    private string GetMonth(string monthNumber)//Получение названия месяца(Оптимизировано)
    {
        if (GetTimeLine().Equals("День"))
        {
            switch (monthNumber)
            {
                case "01": return "января";
                case "02": return "февраля";
                case "03": return "марта";
                case "04": return "апреля";
                case "05": return "мая";
                case "06": return "июня";
                case "07": return "июля";
                case "08": return "августа";
                case "09": return "сентября";
                case "10": return "октября";
                case "11": return "ноября";
                case "12": return "декабря";
                default: return "некорректный номер месяца";
            }
        }
        else //if(GetTimeLine().Equals("Месяц"))
        {
            switch (monthNumber)
            {
                case "01": return "Январь";
                case "02": return "Февраль";
                case "03": return "Март";
                case "04": return "Апрель";
                case "05": return "Май";
                case "06": return "Июнь";
                case "07": return "Июль";
                case "08": return "Август";
                case "09": return "Сентябрь";
                case "10": return "Октябрь";
                case "11": return "Ноябрь";
                case "12": return "Декабрь";
                default: return "некорректный номер месяца";
            }
        }
    }
    private string GetTimeLine()//Получение временного разреза(Оптимизировано)
    {
        return (timelineCombo.SelectedItem as ComboBoxItemPlus)?.Content.ToString()!.Trim() ?? "";
    }

    #endregion Работа со стрококвыми данными

    #region Установка значения

    private void SetTimeLine(string timeLine)//Установка значения для ComboBox разрезов времени(OP)
    {
        foreach (ComboBoxItemPlus item in timelineCombo.Items)
        {
            if (item.Content.Equals(timeLine))
            {
                timelineCombo.SelectedItem = item;
                break;
            }
        }
    }

    #endregion

    #region События

    private void LastDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
    {
        lastDate = lastDatePicker.SelectedDate;
    }
    private void startDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
    {
        startDate = startDatePicker.SelectedDate;
    }
    private void ButtonPlus_Click(object sender, RoutedEventArgs e)
    {
        if (startDatePicker.SelectedDate <= lastDatePicker.SelectedDate)
        {
            if (!string.IsNullOrEmpty(startPriceTextBox.Text) && !string.IsNullOrEmpty(lastPriceTextBox.Text))
            {
                if (decimal.Parse(startPriceTextBox.Text) <= decimal.Parse(lastPriceTextBox.Text))
                {
                    if (parameter.Equals("Цены")) GenerateAveragePricesDataGrid();
                    else GeneratePriceIndexesDataGrid();
                }
                else
                {
                    MessageBox.Show("Промежуток цен выбран неверно!");
                    lastPriceTextBox.Text = startPriceTextBox.Text = string.Empty;
                }

            }
            else
            {
                if (parameter.Equals("Цены")) GenerateAveragePricesDataGrid();
                else GeneratePriceIndexesDataGrid();
            }

        }
    }
    private void categoriesCombo_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (parameter.Equals("Цены"))
        {
            GenerateAveragePricesDataGrid();
        }
        else
        {
            GeneratePriceIndexesDataGrid();
        }
    }
    private void startPriceTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (Decimal.TryParse(startPriceTextBox.Text.Replace(".", ","), out decimal price))
        {
            startPrice = price;
        }
        else
        {
            startPrice = null;
        }
    }
    private void lastPriceTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (Decimal.TryParse(lastPriceTextBox.Text.Replace(".", ","), out decimal price))
        {
            lastPrice = price;
        }
        else
        {
            lastPrice = null;
        }
    }
    private void TextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
    {
        if (!char.IsDigit(e.Text, 0) && e.Text != ",")
        {
            // Если символ не является цифрой или запятой, отменяем ввод
            e.Handled = true;
        }
    }
    private void timelineCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (parameter.Equals("Цены"))
        {
            GenerateAveragePricesDataGrid();
        }
        else
        {
            GeneratePriceIndexesDataGrid();
        }
    }

    #endregion События


}
