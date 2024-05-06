using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PriceAnalysis.Views.Pages;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace PriceAnalysis.ViewModel;

public partial class WelcomeWindowViewModel : ObservableObject
{
    Frame mainFrame;
    public WelcomeWindowViewModel(Frame mainFrame)
    {
        this.mainFrame = mainFrame;
    }


    [RelayCommand]
    void GetAveragePrices()
    {
        mainFrame.Navigate(new DataPage("Цены"));
    }

    [RelayCommand]
    void GetProducts()
    {
        mainFrame.Navigate(new ProductsPage());
    }

    [RelayCommand]
    void GetPriceIndexes()
    {
        mainFrame.Navigate(new DataPage("Индексы"));
    }

    [RelayCommand]
    void GetAveragePricesGraphic()
    {
        mainFrame.Navigate(new GraphicPage("Средние цены"));
    }

    [RelayCommand]
    void GetProductPricesGraphic()
    {
        mainFrame.Navigate(new GraphicPage("Продукты"));
    }

    [RelayCommand]
    void GetSpravka()
    {
        string assemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
        string? projectPath = System.IO.Path.GetDirectoryName(assemblyPath);
        string filePath = System.IO.Path.Combine(projectPath, "СправкаКПП.pdf");
        try
        {
            Process.Start("C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe", filePath);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
        }
    }
}
