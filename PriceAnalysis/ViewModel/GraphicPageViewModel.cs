using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FullControls.Controls;
using LiveCharts;
using LiveCharts.Wpf;
using PriceAnalysis.DataBaseServices;
using PriceAnalysis.Expansion_classes;
using PriceAnalysis.Models;
using System.Collections.ObjectModel;
using System.Windows;

namespace PriceAnalysis.ViewModel;

public partial class GraphicPageViewModel : ObservableObject
{
    private Func<double, string> axisYLabelFormatter;//Форматирование чисел по оси Oy
    private string _graphicParameter = "";

    [ObservableProperty] string pageTittle;
    [ObservableProperty] ComboBoxItemPlus selectedCategory;
    [ObservableProperty] ComboBoxItemPlusWithInfo selectedProduct;
    [ObservableProperty] DateTime? startPriceDate;
    [ObservableProperty] DateTime? endPriceDate;
    [ObservableProperty] DateTime? minDate;

    [ObservableProperty] Visibility productElementsVisibility;

    List<decimal> averagePrices = new List<decimal>();
    List<ProductPrice> productPrices = new List<ProductPrice>();

    [ObservableProperty] SeriesCollection averagePricesCollection;
    [ObservableProperty] ObservableCollection<string> dateLabels;

    public GraphicPageViewModel(string graphicParameter)
    {
        Initialize();
        _graphicParameter = graphicParameter;
    }

    async void Initialize()
    {
        StartPriceDate = await SQLScripts.GetFirstDate();
        EndPriceDate = await SQLScripts.GetLastDate();
        MinDate = StartPriceDate;
        PageTittle = _graphicParameter.Equals("Продукты") ? "График изменения цен продуктов на выбранный промежуток времени" :
                                                            "График изменения средних цен категорий продуктов на выбранный промежуток времени";
        ProductElementsVisibility = _graphicParameter.Equals("Продукты") ? Visibility.Visible : Visibility.Collapsed;
    }

    async Task GetAveragePricesList()//Получение необходимых средних цен(Оптимизировано)
    {
        var allAveragePrices = await SQLScripts.GetAveragePricesAsync();

        averagePrices = allAveragePrices.Where(x => x.CategoryName.Equals(SelectedCategory.Content) &&
        x.AveragePriceDate >= StartPriceDate && x.AveragePriceDate <= EndPriceDate).Select(x => x.Average_Price).ToList();
        SetAxisXValues(allAveragePrices);
    }
    private async Task GetProductPricesList()//Получение необходимых цен продукотв(Оптимизировано)
    {
        var allProductPrices = await SQLScripts.GetAllPricesAsync();

        var selectedProductArticle = SelectedProduct.AdditionalInfo.ToString();

        productPrices = allProductPrices.Where(x => x.ProductId.Equals(selectedProductArticle) &&
                                                    x.PriceDate >= StartPriceDate &&
                                                    x.PriceDate <= EndPriceDate).ToList();
        SetAxisXValues(productPrices);
    }

    public Func<double, string> AxisYLabelFormatter
    {
        get { return axisYLabelFormatter; }
        set
        {
            axisYLabelFormatter = value;
            OnPropertyChanged(nameof(AxisYLabelFormatter));
        }
    }

    // Метод для форматирования значений оси Oy
    private string FormatAxisYLabel(double value)
    {
        return value.ToString("0.00");
    }

    // Установка значений для графика(Optimized)
    void SetData()
    {
        if (_graphicParameter.Equals("Средние цены"))
        {
            if (averagePrices.Count > 0)
            {
                AveragePricesCollection = new SeriesCollection{
                new LineSeries
                {
                    Title = "Средняя цена",
                    Values = new ChartValues<decimal>(averagePrices)
                }};
                AxisYLabelFormatter = FormatAxisYLabel;
            }
            else
            {
                MessageBox.Show("Данных нет!");
            }
        }
        else
        {
            if (productPrices.Count > 0)
            {
                AveragePricesCollection = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Изменение цен продукта",
                    Values = new ChartValues<decimal>(productPrices.Select(x => x.Price))
                }
            };
                AxisYLabelFormatter = FormatAxisYLabel;
            }
            else
            {
                MessageBox.Show("Данных нет!");
            }
        }

    }

    //Установка дат на Оси Ox(Оптимизировано)
    void SetAxisXValues<T>(List<T> dataList)
    {
        if (typeof(T) == typeof(AveragePrice))
        {
            var allAveragePrices = dataList.Cast<AveragePrice>().ToList();
            DateLabels = new ObservableCollection<string>(allAveragePrices.Select(x => x.AveragePriceDate.ToShortDateString()).Distinct());
        }
        else
        {
            var allAveragePrices = dataList.Cast<ProductPrice>().ToList();
            DateLabels = new ObservableCollection<string>(allAveragePrices.Select(x => x.PriceDate.ToShortDateString()).Distinct());
        }
    }

    //Получение графика после нажатия кнопки (Оптимизировано)
    [RelayCommand]
    async Task GetGraphic()
    {
        if (StartPriceDate <= EndPriceDate)
        {
            if (_graphicParameter.Equals("Средние цены"))
            {
                await GetAveragePricesList();
            }
            else
            {
                await GetProductPricesList();
            }
            SetData();
        }
        else
        {
            MessageBox.Show("Неправильный формат выбора дат!");
            StartPriceDate = await SQLScripts.GetFirstDate();
            EndPriceDate = await SQLScripts.GetLastDate();
        }
    }

}
