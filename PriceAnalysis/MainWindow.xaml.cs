using PriceAnalysis.ViewModel;
using System.Windows;

namespace PriceAnalysis;

public partial class MainWindow : Window
{
    MainViewModel viewModel;
    public MainWindow()
    {
        InitializeComponent();
        viewModel = new MainViewModel(passwordBox);
        DataContext = viewModel;
    }

    private void passwordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
       viewModel.Password = passwordBox.Password;
    }

    private void richTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        richTextBox.ScrollToEnd();
    }
}