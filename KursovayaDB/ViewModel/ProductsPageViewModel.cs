using CommunityToolkit.Mvvm.ComponentModel;
using KursovayaDB.DataBaseServices;

namespace KursovayaDB.ViewModel;

public partial class ProductsPageViewModel:ObservableObject
{
    [ObservableProperty] DateTime? selectedProductPricesDate;
    public ProductsPageViewModel()
    {
        Initializing();
    }

    private async void Initializing()//Инициализация компонентов (Оптимизировано)
    {
        SelectedProductPricesDate = await SQLScripts.GetLastDate();
    }
}
