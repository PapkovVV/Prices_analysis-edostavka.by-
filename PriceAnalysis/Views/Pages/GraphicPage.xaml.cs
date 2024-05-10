using FullControls.Controls;
using PriceAnalysis.DataBaseServices;
using PriceAnalysis.Expansion_classes;
using PriceAnalysis.Models;
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
        var uniqueCategories = await SQLScripts.GetAllCategories();

        GenerateCategoriesComboBoxAsync(uniqueCategories);//Генерация КомбоБокса из категорий
    }

    private void GenerateCategoriesComboBoxAsync(IEnumerable<Category> uniqueCategories)//Генерация комбобокса категорий продуктов(Оптимизировано)
    {
        foreach (var category in uniqueCategories)
        {
            categoriesCombo.Items.Add(new ComboBoxItemPlus
            {
                Content = category.Name
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
            productsCombo.Items.Add(new ComboBoxItemPlusWithInfo
            {
                Content = product.Name,
                AdditionalInfo = product.Article
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
