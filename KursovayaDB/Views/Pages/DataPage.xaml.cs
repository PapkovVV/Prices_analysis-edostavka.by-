using FullControls.Controls;
using KursovayaDB.DataBaseServices;
using KursovayaDB.Models;
using KursovayaDB.ViewModel;
using System.Data;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace KursovayaDB.Views.Pages;

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
        var uniqueCategories = await SQLScripts.GetAllCategories();
        foreach (var category in uniqueCategories.Select(x => x.Name))
        {
            categoriesCombo.Items.Add(new ComboBoxItemPlus
            {
                Content = category
            });
        }

        if (parameter.Equals("Цены"))
        {
            GenerateAveragePricesDataGrid();
        }
        else
            GeneratePriceIndexesDataGrid();
    }

    async void GeneratePriceIndexesDataGrid()//Генерация Индексов DataGrid
    {
        var priceIndexes = await SQLScripts.GetAllPriceIndexes();

        List<string> uniqueCategories = await GetNeededCategories();
        if (!string.IsNullOrEmpty(categoriesCombo.Text))
        {
            priceIndexes = GetNeededObjects(priceIndexes);//Отображение данных в соответствии с поиском
        }




        var distinctPriceIndexes = priceIndexes.Where(x => x.IndexDateFrom >= startDate && x.IndexDateTo <= lastDate)
                                    .GroupBy(pi => new { pi.IndexDateFrom, pi.IndexDateTo })
                                    .Select(group => group.First())
                                    .ToList();


        if (distinctPriceIndexes != null && distinctPriceIndexes.Count > 0)
        {
            dataTable = new DataTable();

            dataTable.Columns.Add("Категория", typeof(string));
            foreach (var priceInd in distinctPriceIndexes)
            {
                var dateFrom = RemoveNonNumeric(priceInd.IndexDateFrom.ToShortDateString()).Split(".");
                var dateTo = RemoveNonNumeric(priceInd.IndexDateTo.ToShortDateString()).Split(".");
                var header = $"Период: {dateFrom[0]} {GetMonth(dateFrom[1])} {dateFrom[2]} -" +
                    $" {dateTo[0]} {GetMonth(dateTo[1])} {dateTo[2]}";
                dataTable.Columns.Add(header, typeof(decimal));
            }

            foreach (var category in uniqueCategories)
            {
                dataTable.Rows.Add(dataTable.NewRow()[0] = category);
            }

            foreach (var priceInd in distinctPriceIndexes)
            {
                var dateFrom = RemoveNonNumeric(priceInd.IndexDateFrom.ToShortDateString()).Split(".");
                var dateTo = RemoveNonNumeric(priceInd.IndexDateTo.ToShortDateString()).Split(".");
                var header = $"Период: {dateFrom[0]} {GetMonth(dateFrom[1])} {dateFrom[2]} -" +
                    $" {dateTo[0]} {GetMonth(dateTo[1])} {dateTo[2]}";

                for (int i = 0; i < uniqueCategories.Count; i++)
                {
                    var u = priceIndexes.
                        FirstOrDefault(x => x.CategoryName.Equals(uniqueCategories.ElementAt(i)) &&
                        x.IndexDateFrom == priceInd.IndexDateFrom && x.IndexDateTo == priceInd.IndexDateTo).IndexValue;

                    dataTable.Rows[i][header] = u;
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

            SeTDataGrid();
        }
        else
        {
            dataTable = new DataTable();
            dataGrid.Columns.Clear();
        }
    }

    async void GenerateAveragePricesDataGrid()//Генерация средних цен DataGrid
    {
        var averagePrices = await SQLScripts.GetAveragePricesAsync();

        List<string> uniqueCategories = await GetNeededCategories(); //Категории для отображения в DataGrid

        if (!string.IsNullOrEmpty(categoriesCombo.Text))
        {
            averagePrices = GetNeededObjects(averagePrices);//Отображение данных в соответствии с поиском
        }

        List<DateTime> uniqueDates;

        if (startPrice != null && lastPrice != null)
        {
            uniqueDates = averagePrices.Where(x => x.AveragePriceDate >= startDate && x.AveragePriceDate <= lastDate && x.Average_Price >= startPrice && x.Average_Price <= lastPrice)
                .Select(x => x.AveragePriceDate)
                .OrderBy(x => x).Distinct().TakeLast(7).ToList();
        }
        else if (startPrice != null && lastPrice == null)
        {
            uniqueDates = averagePrices.Where(x => x.AveragePriceDate >= startDate && x.AveragePriceDate <= lastDate && x.Average_Price >= startPrice)
        .Select(x => x.AveragePriceDate)
        .OrderBy(x => x).Distinct().TakeLast(7).ToList();

        }
        else if (startPrice == null && lastPrice != null)
        {
            uniqueDates = averagePrices.Where(x => x.AveragePriceDate >= startDate && x.AveragePriceDate <= lastDate && x.Average_Price <= lastPrice)
.Select(x => x.AveragePriceDate)
.OrderBy(x => x).Distinct().TakeLast(7).ToList();


        }
        else
        {
            uniqueDates = averagePrices.Where(x => x.AveragePriceDate >= startDate && x.AveragePriceDate <= lastDate)
.Select(x => x.AveragePriceDate)
.OrderBy(x => x).Distinct().TakeLast(7).ToList();
        }
        // Создайте DataTable и добавьте столбцы
        if (uniqueDates != null && uniqueDates.Count > 0)
        {
            dataTable = new DataTable();
            dataTable.Columns.Add("Категория", typeof(string));

            foreach (var date in uniqueDates)
            {
                var dateMass = RemoveNonNumeric(date.ToShortDateString()).Split(".");
                var header = $"{dateMass[0]} {GetMonth(dateMass[1])} {dateMass[2]}";
                dataTable.Columns.Add(header, typeof(decimal));
            }

            foreach (var category in uniqueCategories)
            {
                dataTable.Rows.Add(dataTable.NewRow()[0] = category);
            }

            foreach (var date in uniqueDates)
            {
                var dateMass = RemoveNonNumeric(date.ToShortDateString()).Split(".");
                var header = $"{dateMass[0]} {GetMonth(dateMass[1])} {dateMass[2]}";
                for (int i = 0; i < uniqueCategories.Count; i++)
                {
                    if (startPrice == null && lastPrice == null)
                    {
                        var price = averagePrices.
                        FirstOrDefault(x => x.CategoryName.Equals(uniqueCategories.ElementAt(i)) &&
                        x.AveragePriceDate == date);

                        if (price != null)
                        {
                            dataTable.Rows[i][header] = price.Average_Price;
                        }
                    }
                    else if (startPrice != null && lastPrice == null)
                    {
                        var price = averagePrices.
                        FirstOrDefault(x => x.CategoryName.Equals(uniqueCategories.ElementAt(i)) &&
                        x.AveragePriceDate == date && x.Average_Price >= startPrice);
                        if (price != null)
                        {
                            dataTable.Rows[i][header] = price.Average_Price;
                        }
                    }
                    else if (startPrice == null && lastPrice != null)
                    {
                        var price = averagePrices.
                        FirstOrDefault(x => x.CategoryName.Equals(uniqueCategories.ElementAt(i)) &&
                        x.AveragePriceDate == date && x.Average_Price <= lastPrice);

                        if (price != null)
                        {
                            dataTable.Rows[i][header] = price.Average_Price;
                        }
                    }
                    else
                    {
                        var price = averagePrices.
                        FirstOrDefault(x => x.CategoryName.Equals(uniqueCategories.ElementAt(i)) &&
                        x.AveragePriceDate == date && x.Average_Price >= startPrice && x.Average_Price <= lastPrice);

                        if (price != null)
                        {
                            dataTable.Rows[i][header] = price.Average_Price;
                        }
                    }
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

            // Теп
            SeTDataGrid();

        }
        else
        {
            dataTable = new DataTable();
            dataGrid.Columns.Clear();
        }

    }

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
    private List<T> GetNeededObjects<T>(List<T> dataList)
    {
        List<T> result = new List<T>();//Список результата

        if (typeof(T) == typeof(AveragePrice))
        {
            List<AveragePrice> averagePrices = dataList.Cast<AveragePrice>().ToList();
            if (averagePrices.Any(x => x.CategoryName.ToLower().StartsWith(categoriesCombo.Text.ToLower())))
            {
                result = averagePrices.Where(x => x.CategoryName.ToLower().StartsWith(categoriesCombo.Text.ToLower())).Cast<T>().ToList();
            }
        }
        else
        {
            List<PriceIndex> priceIndexes = dataList.Cast<PriceIndex>().ToList();
            if (priceIndexes.Any(x => x.CategoryName.ToLower().StartsWith(categoriesCombo.Text.ToLower())))
            {
                result = priceIndexes.Where(x => x.CategoryName.ToLower().StartsWith(categoriesCombo.Text.ToLower())).Cast<T>().ToList();
            }
        }

        return result;
    }

    #endregion Все для генерации DataGrid с необходимыми данными




    void SeTDataGrid()//Генерация таблицы для отображения необходимых данных
    {
        dataGrid.ItemsSource = dataTable.DefaultView;
        foreach (var column in dataGrid.Columns)
        {
            column.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
        }

        for (int i = 1; i < dataGrid.Columns.Count; i++)
        {
            var column = dataGrid.Columns[i];
            if (column is DataGridTextColumn textColumn)
            {
                textColumn.ElementStyle = new Style(typeof(TextBlock))
                {
                    Setters =
            {
                new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right)
            }
                };
                column.HeaderStyle = new Style(typeof(DataGridColumnHeader))
                {
                    Setters =
            {
                new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Right)
            }
                };
            }
        }
    }

    static string RemoveNonNumeric(string input)
    {
        string result = Regex.Replace(input, @"[^\d.]", "");
        return result;
    }

    static string GetMonth(string monthNumber)
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
            dataTable = new DataTable();
            GenerateAveragePricesDataGrid();
        }
        else GeneratePriceIndexesDataGrid();
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
}
