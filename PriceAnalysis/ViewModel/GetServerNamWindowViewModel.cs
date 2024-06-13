using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PriceAnalysis.DataBaseServices;
using PriceAnalysis.Services.ServerServices;
using System.Windows;

namespace PriceAnalysis.ViewModel;

public partial class GetServerNamWindowViewModel : ObservableObject
{
    [ObservableProperty] string serverName = null!;
    public event Action CloseWindow;
    private bool _isChanging = false;

    public GetServerNamWindowViewModel(bool isChanging)
    {
        _isChanging = isChanging;
    }
    [RelayCommand]
    private void SaveServerName()
    {
        if (!string.IsNullOrEmpty(ServerName))
        {
            ServerFile.SaveServerName(ServerName.Trim());

            string resultMessage = "";
            bool result = SQLScripts.IsConnectionValid(out resultMessage);

            MessageBox.Show(resultMessage);
            if (!result)
            {
                ServerName = string.Empty;
            }
            else
            {
                CloseWindow?.Invoke();
            }
        }
        else
        {
            MessageBox.Show("Необходимо ввести имя сервера!", "", MessageBoxButton.OK,MessageBoxImage.Warning);
        }
    }

    [RelayCommand]
    private void CloseApplication()
    {
        if (_isChanging)
        {
            CloseWindow?.Invoke();
        }
        else
        {
            Application.Current.Shutdown();
        }
    }
}
