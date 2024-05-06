using FullControls.Controls;
using PriceAnalysis.DataBaseServices;
using PriceAnalysis.ViewModel;
using System.Windows;
using System.Windows.Controls;

namespace PriceAnalysis.Views.Pages;

public partial class GraphicPage : Page
{
    private string _graphicParamater = "";
    public GraphicPage(string graphicParamater)
    {
        InitializeComponent();
        Initialize();
        DataContext = new GraphicPageViewModel(graphicParamater);
        _graphicParamater = graphicParamater;
    }

    async void Initialize()
    {
        var uniqueCategories = (await SQLScripts.GetAllCategories()).Select(x => x.Name).ToList();

        GenerateCategoriesComboBox(uniqueCategories);//Генерация КомбоБокса из категорий
    }

    private void GenerateCategoriesComboBox(IEnumerable<string> uniqueCategories)//Генерация комбобокса категорий продуктов(Оптимизировано)
    {
        foreach (var category in uniqueCategories)
        {
            categoriesCombo.Items.Add(new ComboBoxItemPlus
            {
                Content = category
            });
        }
    }
    private async void GenerateProductsComboBox()//Генерация комбобокса продуктов
    {
        var selectedCategory = (categoriesCombo.SelectedItem as ComboBoxItemPlus)?.Content.ToString();

        var categoryId = (await SQLScripts.GetAllCategories()).FirstOrDefault(x => x.Name.Equals(selectedCategory))!.Id;
        var allProducts = (await SQLScripts.GetAllProducts())?.Where(x => x.CategoryId == categoryId)!;//Получаем все продукты

        foreach (var product in allProducts)
        {
            productsCombo.Items.Add(new ComboBoxItemPlus
            {
                Content = product.Name
            });
        }
        productsCombo.SelectedIndex = 0;
    }

    private void categoriesCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_graphicParamater.Equals("Продукты"))
        {
            productsCombo.Items.Clear();
            GenerateProductsComboBox();
        }
    }
}
