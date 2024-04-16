using FullControls.Controls;
using KursovayaDB.DataBaseServices;
using KursovayaDB.Models;
using KursovayaDB.ViewModel;
using System.Diagnostics;
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
        DataContext = new ProductsPageViewModel();
        InitializeComponent();
        Generate();
    }

    async void Generate()
    {
        try
        {
            await InitializeLists();
            await Task.WhenAll(GenerateCategoriesCombo(), GeneratePricesDatesCombo());
        }
        finally
        {
            CreateAccordion(DateTime.Now.Date);
        }
    }

    async Task InitializeLists()//Инициализация списков (Оптимизировано)
    {
        var getAllCategoriesTask = SQLScripts.GetAllCategories();
        var getAllProductsTask = SQLScripts.GetAllProducts();
        var getAllAttributesTask = SQLScripts.GetAllAttributes();
        var getAllAttributeValuesTask = SQLScripts.GetAllAttributeValues();
        var getAllPricesTask = SQLScripts.GetAllPricesAsync();

        await Task.WhenAll(getAllCategoriesTask, getAllProductsTask, getAllAttributesTask, getAllAttributeValuesTask, getAllPricesTask);

        uniqueCategories = await getAllCategoriesTask;
        uniqueProducts = await getAllProductsTask;
        uniqueAttributes = await getAllAttributesTask;
        attributeValues = await getAllAttributeValuesTask;
        productPrices = await getAllPricesTask;
    }
    private void CreateAccordion(DateTime? pricesDate)//Создание аккордиона
    {
        var accordionItemsCollection = accordion.Items;//Получаем коллкцию элементов 

        foreach (var category in uniqueCategories.OrderBy(x => x.Id))
        {
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

                var productPrice = productPrices.Where(x => x.PriceDate.Date == pricesDate?.Date).FirstOrDefault(x => x.ProductId.Equals(product.Article));//Объект цены продукта

                List<string> attribute_value = new List<string>();

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

    private async Task GenerateCategoriesCombo()//Генерация элементов ComboBox категорий (Оптимизировано)
    {
        foreach (var category in uniqueCategories)
        {
            categoriesCombo.Items.Add(new ComboBoxItemPlus
            {
                Content = category.Name
            });
        }
    }

    private async Task GeneratePricesDatesCombo()//Генерация элементов ComboBox ценовых дат (Оптимизировано)
    {
        List<DateTime> uniqueDates = productPrices.Select(x => x.PriceDate).Distinct().ToList();

        foreach (var date in uniqueDates)
        {
            pricesDatesCombo.Items.Add(new ComboBoxItemPlus
            {
                Content = date.Date.ToString("dd MMMM yyyy")
            });
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
