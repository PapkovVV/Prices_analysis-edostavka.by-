using FullControls.Controls;
using KursovayaDB.DataBaseServices;
using KursovayaDB.ViewModel;
using System.Windows.Controls;

namespace KursovayaDB.Views.Pages;

public partial class GraphicPage : Page
{
    public GraphicPage()
    {
        InitializeComponent();
        Initialize();
        DataContext = new GraphicPageViewModel();
    }

    async void Initialize()
    {
        var uniqueCategories = (await SQLScripts.GetAllCategories()).Select(x => x.Name).ToList();

        foreach (var category in uniqueCategories)
        {
            categoriesCombo.Items.Add(new ComboBoxItemPlus
            {
                Content = category
            });
        }
    }
}
