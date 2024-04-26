using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FullControls.Controls;
using PriceAnalysis.DataBaseServices;
using PriceAnalysis.Models;
using LiveCharts;
using LiveCharts.Wpf;
using System.Collections.ObjectModel;
using System.Windows;

namespace PriceAnalysis.ViewModel;

public partial class GraphicPageViewModel : ObservableObject
{
    private Func<double, string> axisYLabelFormatter;//Форматирование чисел по оси Oy

    [ObservableProperty] ComboBoxItemPlus selectedCategory;
    [ObservableProperty] DateTime? startPriceDate;
    [ObservableProperty] DateTime? endPriceDate;
    [ObservableProperty] DateTime? minDate;

    List<decimal> averagePrices = new List<decimal>();
    [ObservableProperty] SeriesCollection averagePricesCollection;
    [ObservableProperty] ObservableCollection<string> dateLabels;

    public GraphicPageViewModel()
    {
        Initialize();
    }

    async void Initialize()
    {
        StartPriceDate = await SQLScripts.GetFirstDate();
        EndPriceDate = await SQLScripts.GetLastDate();
        MinDate = StartPriceDate;
    }

    async Task GetPricesList()
    {
        var allAveragePrices = await SQLScripts.GetAveragePricesAsync();

        averagePrices = allAveragePrices.Where(x =>x.CategoryName.Equals(SelectedCategory.Content) &&
        x.AveragePriceDate >= StartPriceDate && x.AveragePriceDate <= EndPriceDate).Select(x => x.Average_Price).ToList();
        SetAxisXValues(allAveragePrices);
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

    // Метод для форматирования значений оси Y
    private string FormatAxisYLabel(double value)
    {
        return value.ToString("0.00");
    }

    void SetData()// Установка значений для графика(Optimized)
    {
        if (averagePrices.Count > 0)
        {
            AveragePricesCollection = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Средняя цена",
                    Values = new ChartValues<decimal>(averagePrices)
                }
            };
            AxisYLabelFormatter = FormatAxisYLabel;
        }
        else
        {
            MessageBox.Show("Данных нет!");
        }
    }

    void SetAxisXValues(List<AveragePrice> allAveragePrices)
    {
        DateLabels = new ObservableCollection<string> (allAveragePrices.Select(x => x.AveragePriceDate.ToShortDateString()).Distinct());
    }

    [RelayCommand]
    async void GetGraphic()
    {
        if (StartPriceDate <= EndPriceDate)
        {
            await GetPricesList();
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
