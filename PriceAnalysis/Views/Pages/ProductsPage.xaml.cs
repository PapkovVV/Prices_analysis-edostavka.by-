using FullControls.Controls;
using PriceAnalysis.DataBaseServices;
using PriceAnalysis.Models;
using PriceAnalysis.ViewModel;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PriceAnalysis.Views.Pages;
public partial class ProductsPage : Page
{
    private bool isUserClick = false;

    DateTime? pricesDate = null;
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
            CreateAccordion();
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
    private void CreateAccordion()//Создание аккордиона
    {
        var accordionItemsCollection = accordion.Items;//Получаем коллкцию элементов 

        var requiredCategories = string.IsNullOrEmpty(categoriesCombo.Text) ? uniqueCategories : uniqueCategories.Where(x => x.Name.StartsWith(categoriesCombo.Text));

        foreach (var category in requiredCategories.OrderBy(x => x.Id))
        {
            var catItem = new SimpleAccordionItem
            {
                Header = category.Name,
                Margin = new Thickness(0, 0, 0, 20),
                IsExpanded = false
            };

            catItem.Content = new ItemsControl()
            {
                ItemsSource = CreateProductsAccordionItems(category.Id)
            };

            accordionItemsCollection.Add(catItem);
        }
    }

    private List<SimpleAccordionItem> CreateProductsAccordionItems(int categoryId)//Создание списка продуктов для определенной категории
    {
        pricesDate = pricesDate ?? DateTime.Now.Date;
        List<SimpleAccordionItem> products = new List<SimpleAccordionItem>();//Список элементов продуктов
        List<string> neededArticles = productPrices.Where(x => x.PriceDate.Date == pricesDate?.Date).Select(x => x.ProductId).ToList();//Артикулы продуктов для отображения
        List<ProductName> neededProducts = uniqueProducts.Where(x => x.CategoryId == categoryId && neededArticles.Contains(x.Article)).ToList();//Необходимые продукты

        foreach (var product in neededProducts)
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
        return products;
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

        pricesDatesCombo.SelectedIndex = uniqueDates.Count - 1;
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

    private void pricesDatesCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (isUserClick)
        {
            var selectedItem = pricesDatesCombo.SelectedItem as ComboBoxItemPlus;
            if (selectedItem != null)
            {
                pricesDate = DateTime.ParseExact(selectedItem.Content.ToString().Trim(), "dd MMMM yyyy", CultureInfo.CurrentCulture);
                if (accordion.HasItems)
                {
                    accordion.Items.Clear();
                    CreateAccordion();
                }
            }
        }
        isUserClick = false;
    }

    private void pricesDatesCombo_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        isUserClick = true;
    }
}
