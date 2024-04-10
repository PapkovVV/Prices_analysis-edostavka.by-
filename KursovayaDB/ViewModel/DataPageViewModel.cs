using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using KursovayaDB.DataBaseServices;
using KursovayaDB.Services.ExportServices;
using KursovayaDB.Views.Windows;
using System.Windows;
using System.Windows.Controls;

namespace KursovayaDB.ViewModel;

public partial class DataPageViewModel : ObservableObject
{
    public DataGrid DataGrid { get; set; }
    string parameter = string.Empty;

    [ObservableProperty] string pageTitle;
    [ObservableProperty] DateTime? startDate;
    [ObservableProperty] DateTime? lastDate;
    [ObservableProperty] DateTime? minimalDate;
    [ObservableProperty] Visibility priceLabelVisibility;
    [ObservableProperty] Visibility pricesStackVisibility;
    public DataPageViewModel(string parameter)
    {
        this.parameter = parameter;
        Initialize();
    }

    async void Initialize()
    {
        if (parameter.Equals("Цены"))
        {
            PageTitle = "СРЕДНИЕ  ЦЕНЫ  НА  ТОВАРЫ, РЕАЛИЗУЕМЫЕ В РОЗНИЧНОЙ СЕТИ\n" +
            "(на торговой плозадке Евроопт)\n" +
            "(в рублях за килограмм, литр, десяток, изделие)\n";
            PriceLabelVisibility = Visibility.Visible;
            PricesStackVisibility = Visibility.Visible;
        }
        else
        {
            PageTitle = "ИНДЕКСЫ  ПОТРЕБИТЕЛЬСКИХ ЦЕН\n";
            PriceLabelVisibility = Visibility.Collapsed;
            PricesStackVisibility = Visibility.Collapsed;
        }
        StartDate = await SQLScripts.GetFirstDate();
        LastDate = await SQLScripts.GetLastDate();
        MinimalDate = StartDate;
    }

    [RelayCommand]
    void PrintExcel()
    {
        var promptWindow = new PromptWindow();
        promptWindow.ShowDialog();
        bool result = promptWindow.Result;

        if (promptWindow.WasClosedByUser)
        {
            if (parameter.Equals("Цены"))
            {
                ExcelExport.ExcelImportAndOpen(DataGrid, "AveragePrices", PageTitle, result);
            }
            else
            {
                ExcelExport.ExcelImportAndOpen(DataGrid, "PriceIndexes", PageTitle, result);
            }
        }
    }

    [RelayCommand]
    void PrintWord()
    {
        var promptWindow = new PromptWindow();
        promptWindow.ShowDialog();
        bool result = promptWindow.Result;

        if (parameter.Equals("Цены"))
        {
            WordExport.WordImportAndOpen(DataGrid, "AveragePrices", PageTitle, result);
        }
        else
        {
            WordExport.WordImportAndOpen(DataGrid, "PriceIndexes", PageTitle, result);
        }
    }
}
