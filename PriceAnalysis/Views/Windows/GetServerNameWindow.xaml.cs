using FullControls.SystemComponents;
using PriceAnalysis.ViewModel;
using System.Windows;

namespace PriceAnalysis.Views.Windows;

/// <summary>
/// Логика взаимодействия для GetServerNameWindow.xaml
/// </summary>
public partial class GetServerNameWindow : FlexWindow
{
    public GetServerNameWindow()
    {
        InitializeComponent();
        DataContext = new GetServerNamWindowViewModel();
        if (DataContext is GetServerNamWindowViewModel viewModel)
        {
            viewModel.CloseWindow += () => this.Close();
        }
    }
}
