using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FullControls.Controls;
using PriceAnalysis.DataBaseServices;
using PriceAnalysis.Services.ExportServices;
using PriceAnalysis.Views.Windows;
using System.Windows;
using System.Windows.Controls;

namespace PriceAnalysis.ViewModel;

public partial class DataPageViewModel : ObservableObject
{
    public DataGrid DataGrid { get; set; }
    string parameter = string.Empty;

    [ObservableProperty] string pageTitle;
    [ObservableProperty] string helpText;
    [ObservableProperty] ComboBoxItemPlus timeline;
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
            PageTitle = "ОТЧЕТ ПО СРЕДНИМ  ЦЕНАМ  НА  ТОВАРЫ, РЕАЛИЗУЕМЫМ В РОЗНИЧНОЙ СЕТИ\n" +
            "(на торговой площадке Евроопт)\n" +
            "(в рублях за килограмм, литр, десяток, изделие)";
            HelpText = "\tСредние цены на продукты на сайте edostavka.by представляют собой средневзвешенные значения стоимости товаров, " +
                "рассчитанные на основе цен от различных продавцов, доступных для онлайн-заказа. " +
                "Они позволяют потребителям оценить среднюю стоимость продуктов и сравнить ее с другими торговыми площадками, " +
                "что помогает принимать более осознанные решения о покупках.";
            PriceLabelVisibility = Visibility.Visible;
            PricesStackVisibility = Visibility.Visible;
        }
        else
        {
            PageTitle = "ОТЧЕТ ПО ИНДЕКСАМ ПОТРЕБИТЕЛЬСКИХ ЦЕН";
            HelpText = "\tИндексы потребительских цен на сайте edostavka.by представляют собой показатели, " +
                "отражающие изменения цен на потребительские товары и услуги в определенном временном периоде. " +
                "Они рассчитываются на основе средних цен на определенный набор товаров и услуг, " +
                "собранных из различных источников, и помогают анализировать тенденции в ценообразовании, " +
                "инфляции и покупательской активности.";
            PriceLabelVisibility = Visibility.Collapsed;
            PricesStackVisibility = Visibility.Collapsed;
        }
        StartDate = await SQLScripts.GetFirstDate();
        LastDate = await SQLScripts.GetLastDate();
        MinimalDate = StartDate;
    }

    [RelayCommand]
    async Task PrintExcel()
    {
        var promptWindow = new PromptWindow();
        promptWindow.ShowDialog();
        bool result = promptWindow.Result;

        if (promptWindow.WasClosedByUser)
        {
            if (parameter.Equals("Цены"))
            {
                await ExcelExport.ExcelImportAndOpenAsync(DataGrid, "AveragePrices", PageTitle, GetTimeline(), result);
            }
            else
            {
                await ExcelExport.ExcelImportAndOpenAsync(DataGrid, "PriceIndexes", PageTitle, GetTimeline(), result);
            }
        }
    }

    [RelayCommand]
    async Task PrintWord()
    {
        var promptWindow = new PromptWindow();
        promptWindow.ShowDialog();
        bool result = promptWindow.Result;

        if (parameter.Equals("Цены"))
        {
            await WordExport.WordImportAndOpen(DataGrid, "AveragePrices", PageTitle, GetTimeline(), result);
        }
        else
        {
            await WordExport.WordImportAndOpen(DataGrid, "PriceIndexes", PageTitle, GetTimeline(), result);
        }

    }

    private string GetTimeline()//Получение временного отрезка(Оптимизировано)
    {
        return Timeline.Content.ToString()!;
    }
}
