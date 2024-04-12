using FullControls.Controls;
using KursovayaDB.DataBaseServices;
using KursovayaDB.Models;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace KursovayaDB.Views.Pages;
public partial class ProductsPage : Page
{
    List<Category> uniqueCategories;
    List<ProductName> uniqueProducts;
    List<ProductAttribute> uniqueAttributes;
    List<AttributeValues> attributeValues;
    List<ProductPrice> productPrices;
    public ProductsPage()
    {
        InitializeComponent();
        GenerateDataGrid();
    }

    async void GenerateDataGrid()
    {
        try
        {
            await InitializeLists();
        }
        finally
        {
            Generating();
        }
    }

    async Task InitializeLists()
    {
        uniqueCategories = await SQLScripts.GetAllCategories();
        uniqueProducts = await SQLScripts.GetAllProducts();
        uniqueAttributes = await SQLScripts.GetAllAttributes();
        attributeValues = await SQLScripts.GetAllAttributeValues();
        productPrices = await SQLScripts.GetAllPricesAsync();
    }
    void Generating()
    {

        var accordionItemsCollection = accordion.Items;//Получаем коллкцию элементов 

        foreach (var category in uniqueCategories.OrderBy(x => x.Id))
        {
            categoriesCombo.Items.Add(new ComboBoxItemPlus
            {
                Content = category.Name
            });

            var catItem = new SimpleAccordionItem
            {
                Header = category.Name,
                Margin = new Thickness(0, 0, 0, 20),
                IsExpanded = false
            };

            List<SimpleAccordionItem> products = new List<SimpleAccordionItem>();
            foreach (var product in uniqueProducts.Where(x => x.CategoryId == category.Id))
            {
                var productItem = new SimpleAccordionItem
                {
                    Header = product.Name,
                    HeaderBackground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ebebf5")),
                    HeaderBackgroundOnMouseOver = System.Windows.Media.Brushes.White,
                    HeaderBackgroundOnExpanded = System.Windows.Media.Brushes.White,
                    HeaderBackgroundOnMouseOverOnExpanded = System.Windows.Media.Brushes.White,
                    Margin = new Thickness(5),
                    IsExpanded = false,
                };

                List<string> attribute_value = new List<string>();
                var productPrice = productPrices.FirstOrDefault(x => x.ProductId.Equals(product.Article));
                if (productPrice != null)
                {
                    foreach (var attributeValue in attributeValues.Where(x => x.ProductId.Equals(product.Article)))
                    {
                        var attr = uniqueAttributes.FirstOrDefault(x => x.Id == attributeValue.AttributeId).Name;
                        attribute_value.Add($"{attr}: {attributeValue.Value}");
                    }

                    attribute_value.Add($"Цена за 1 кг: {productPrice.Price} руб.");

                    productItem.Content = new ItemsControl()
                    {
                        ItemsSource = attribute_value
                    };
                    products.Add(productItem);
                }
                
            }
            catItem.Content = new ItemsControl()
            {
                ItemsSource = products
            };

            accordionItemsCollection.Add(catItem);
        }

    }

    private void categoriesCombo_TextChanged(object sender, TextChangedEventArgs e)
    {
        foreach (AccordionItem item in accordion.Items)
        {
            item.Visibility = Visibility.Visible;
        }

        if (!string.IsNullOrEmpty(categoriesCombo.Text))
        {
            foreach (AccordionItem item in accordion.Items)
            {
                if (!item.Header.ToString().ToLower().StartsWith(categoriesCombo.Text.ToLower()))
                {
                    item.Visibility = Visibility.Collapsed;
                }
            }
        }
    }
}
