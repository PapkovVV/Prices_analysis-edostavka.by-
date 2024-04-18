using HtmlAgilityPack;
using KursovayaDB.Models;
using KursovayaDB.Services.LogServices;
using KursovayaDB.ViewModel;
using System.Text.RegularExpressions;
using System.Windows;

namespace KursovayaDB.Services.ParsingServices;

public class Parsing
{
    private static MainViewModel mainViewModelInstance = null!;
    private static HtmlWeb htmlWeb = new HtmlWeb();

    #region Списки

    public static List<Category> categories = new List<Category>();//Список спаршенных категорий
    public static List<ProductAttribute> productAttributes = new List<ProductAttribute>();//Список спаршенных названий характеристик
    public static List<AttributeValues> attributeValues = new List<AttributeValues>();//Список спаршенных значений хаарктеристик

    #endregion Списки

    #region Блокировщики

    private static readonly SemaphoreSlim semaphore = new SemaphoreSlim(5);//Количество одновременных запросов
    private static readonly SemaphoreSlim productsLock = new SemaphoreSlim(1);
    private static readonly SemaphoreSlim attributesLock = new SemaphoreSlim(1);
    private static readonly SemaphoreSlim attributeValuesLock = new SemaphoreSlim(1);

    #endregion Блокировщики

    public static async Task ParseAllAsync(MainViewModel mainVMI)//Парсинг всего(Оптимизировано)
    {
        try
        {
            mainViewModelInstance = mainVMI;
            HtmlDocument doc = new HtmlDocument();

            await Task.WhenAll(ParseWheatFlour(), ParseSugar(), ParseSourCream(), ParseKvas(),
                ParseUksus(), ParseHalva(), ParseApples(), ParseTomatoSouse(), ParseKrupaGrechnevaya(), ParseHoney());
        }
        catch (Exception ex)
        {
            await LogFile.AddLogMessageAsync("ParseAllAsync", ex.Message, mainViewModelInstance, true);
        }
    }

    #region Парсинг опрделенных категорий продуктов

    public static async Task ParseWheatFlour()//Парсинг категории "Мука пшеничная"(OP)
    {
        await ParseCategoryAsync("https://edostavka.by/category/5152?lc=2");
    }
    public static async Task ParseSugar()//Парсинг категории "Сахар"(ОР)
    {
        await ParseCategoryAsync("https://edostavka.by/category/5632");
    }
    public static async Task ParseSourCream()//Парсинг категории "Сметана"(ОР)
    {
        await ParseCategoryAsync("https://edostavka.by/category/5097?lc=2");
    }
    public static async Task ParseKvas()//Парсинг категории "Квас"(ОР)
    {
        await ParseCategoryAsync("https://edostavka.by/category/4973#modal-opened&product_preview=null");
    }
    public static async Task ParseUksus()//Парсинг категории "Уксус"(ОР)
    {
        await ParseCategoryAsync("https://edostavka.by/category/4943#modal-opened&product_preview=null");
    }
    public static async Task ParseKrupaGrechnevaya()//Парсинг категории "Крупа гречневая"(ОР)
    {
        await ParseCategoryAsync("https://edostavka.by/category/5225#modal-opened&product_preview=null");
    }
    public static async Task ParseHalva()//Парсинг категории "Халва"(ОР)
    {
        await ParseCategoryAsync("https://edostavka.by/category/5004#modal-opened&product_preview=null");
    }
    public static async Task ParseApples()//Парсинг категории "Яблоки"(ОР)
    {
        await ParseCategoryAsync("https://edostavka.by/category/5293");
    }
    public static async Task ParseTomatoSouse()//Парсинг категории "Томатный соус"(ОР)
    {
        await ParseCategoryAsync("https://edostavka.by/category/5254");
    }
    public static async Task ParseHoney()//Парсинг категории "Мед"(ОР)
    {
        await ParseCategoryAsync("https://edostavka.by/category/5647?id=5647&filter=%7B%22Filters%22" +
            "%3A%5B%7B%22PropertyId%22%3A149%2C%22PropertyValueId%22%3A%5B17384%5D%2C%22PropertyValueFloat%2" +
            "2%3A%22%22%2C%22PropertyValueMin%22%3A%22%22%2C%22PropertyValueMax%22%3A%22%22%7D%5D%7D");
    }

    #endregion Парсинг опрделенных категорий продуктов


    #region Общий парсинг

    static async Task ParseCategoryAsync(string categoryUrl)//Парсинг категории(Оптимизировано)
    {
        int currentAttempt = 0;

        while (currentAttempt < 10)
        {
            try
            {
                var newCategory = new Category { Name = await GetProductCategoryName(htmlWeb, categoryUrl) };

                categories.Add(newCategory);

                var evroProducts = (await htmlWeb.LoadFromWebAsync(categoryUrl).ConfigureAwait(false)).DocumentNode
                            .SelectNodes("//div[@class='adult-wrapper_adult__eCCJW vertical_product__Q8mUI']");

                if (evroProducts != null)
                {
                    var tasks = new List<Task>();

                    foreach (var product in evroProducts)
                    {
                        string href = "https://edostavka.by" + product.SelectSingleNode(".//a[@class='card-image_link__lTrk0']").GetAttributeValue("href", "");

                        if (!string.IsNullOrEmpty(href))
                        {
                            tasks.Add(ParseProductAsync(href, newCategory));
                        }
                    }
                    await Task.WhenAll(tasks);

                    foreach (var newProduct in newCategory.products)
                    {
                        await ParceAttributeValueAsync("https://edostavka.by/product/" + newProduct.Article, newProduct);
                    }
                }
                else
                {
                    MessageBox.Show("Данные продуктов не найдены!");
                }
                break;
            }
            catch (Exception ex)
            {
                currentAttempt++;
                await LogFile.AddLogMessageAsync("ParseCategoryAsync", $"Ошибка {ex.Message} при загрузке страницы {categoryUrl}. Повторная попытка {currentAttempt}", mainViewModelInstance, true);
            }
        }
    }
    static async Task ParseProductAsync(string href, Category category)//Парсинг продукта(Оптимизировано)
    {
        int currentAttempt = 0;

        while (currentAttempt < 10)
        {
            try
            {
                await semaphore.WaitAsync();

                await LogFile.AddLogMessageAsync("ParseProductAsync", $"Попытка загрузить стрaницу: {href}", mainViewModelInstance);
                var doc = await htmlWeb.LoadFromWebAsync(href).ConfigureAwait(false);
                await LogFile.AddLogMessageAsync("ParseProductAsync", $"Страница {href} загружена успешно!", mainViewModelInstance);

                string productName = await GetProductName(doc);//Получаем название продукта

                decimal productPrice = await GetProductPrice(doc, productName);//Получаем цену продукта

                var newProduct = new ProductName { CategoryId = category.Id, Name = productName, Article = await GetArticleFromUrl(href) };
                newProduct.ProductPrice = new ProductPrice { ProductId = newProduct.Article, Price = productPrice };

                await productsLock.WaitAsync();
                try
                {
                    category.products.Add(newProduct);
                }
                finally
                {
                    productsLock.Release();
                }

                await ParseAttributeAsync(doc);

                await Task.Delay(1000);
                break;
            }
            catch (Exception ex)
            {
                currentAttempt++;
                await LogFile.AddLogMessageAsync("ParseProductAsync", $"Ошибка {ex.Message} при загрузке страницы {href}. Повторная попытка {currentAttempt}", mainViewModelInstance, true);
            }
            finally
            {
                semaphore.Release();
            }
        }
    }
    static async Task ParseAttributeAsync(HtmlDocument? doc)//Парсинг названий характеристик
    {
        try
        {
            var attributeContainer = await GetAttributeContainer(doc);

            if (attributeContainer != null)
            {
                var evroAttributes = await GetAttributes(attributeContainer);

                if (evroAttributes != null || evroAttributes.Any())
                {
                    foreach (var attribute in evroAttributes)
                    {
                        var attributeName = await GetAttributeName(attribute);
                        if (!string.IsNullOrEmpty(attributeName))
                        {
                            await attributesLock.WaitAsync();
                            try
                            {
                                if (!productAttributes.Any(x => x.Name.Equals(attributeName)))
                                {
                                    productAttributes.Add(new ProductAttribute
                                    {
                                        Name = attributeName
                                    });
                                }
                            }
                            finally
                            {
                                attributesLock.Release();
                            }
                        }
                    }
                }
            }

        }
        catch (Exception ex)
        {
            await LogFile.AddLogMessageAsync("ParseAttributeAsync", ex.Message, mainViewModelInstance, true);
        }
    }
    static async Task ParceAttributeValueAsync(string href, ProductName product)//Парсинг значений характеристик
    {
        var doc = await htmlWeb.LoadFromWebAsync(href).ConfigureAwait(false);

        var attributeContainer = await GetAttributeContainer(doc);

        if (attributeContainer != null)
        {
            var evroAttributes = await GetAttributes(attributeContainer);

            if (evroAttributes != null || evroAttributes.Any())
            {
                foreach (var attribute in evroAttributes)
                {
                    var attributeName = await GetAttributeName(attribute);
                    var attributeValue = await GetAttributeValue(attribute);

                    if (!string.IsNullOrEmpty(attributeValue))
                    {
                        await attributeValuesLock.WaitAsync();
                        try
                        {
                            if (!attributeName.Contains("товар"))
                            {
                                var newAttrValue = new AttributeValues
                                {
                                    ProductId = product.Article,
                                    Value = attributeValue
                                };


                                foreach (var attr in productAttributes)
                                {
                                    if (attr.Name.Equals(attributeName))
                                    {
                                        newAttrValue.AttributeId = attr.Id;
                                        newAttrValue.AttributeName = attr.Name;
                                    }
                                }

                                attributeValues.Add(newAttrValue);

                            }
                        }
                        finally
                        {
                            attributeValuesLock.Release();
                        }
                    }
                }
            }
        }
    }

    #endregion Общий парсинг


    #region Получение строковых значений
    static async Task<string> GetProductCategoryName(HtmlWeb htmlWeb, string categoryUrl)//Метод получения категории (Оптимизировано)
    {
        try
        {
            var doc = await htmlWeb.LoadFromWebAsync(categoryUrl).ConfigureAwait(false);

            var categoryNameNode = doc.DocumentNode.SelectSingleNode("//h1[@class='typography typography_tag_h1 typography_weight_700 " +
                "heading_heading__text__gP6AN heading_heading__text_level_1__7_duQ']");

            if (categoryNameNode != null)
            {
                string categoryName = categoryNameNode.InnerText;

                if (!string.IsNullOrEmpty(categoryName))
                {
                    return categoryName.Trim();
                }
            }
        }
        catch (Exception ex)
        {
            await LogFile.AddLogMessageAsync("GetProductCategoryName", ex.Message, mainViewModelInstance, true);
        }
        return "Неизвестно";
    }
    static async Task<string> GetProductName(HtmlDocument doc)//Метод получения названия продукта (Оптимизировано)
    {
        try
        {
            var productNameNode = doc.DocumentNode.SelectSingleNode("//h1[@class='typography typography_tag_h1 typography_weight_700 heading_heading__text__gP6AN" +
                             " heading_heading__text_level_1__7_duQ']");

            if (productNameNode != null)
            {
                string productName = productNameNode.InnerText;

                if (!string.IsNullOrEmpty(productName))
                {
                    return productName.Trim().Replace("100х5", "").Replace("&#x27;", "'");
                }
            }
        }
        catch (Exception ex)
        {
            await LogFile.AddLogMessageAsync("GetProductName", ex.Message, mainViewModelInstance, true);
        }

        return "Неизвестно";
    }
    static async Task<decimal> GetProductPrice(HtmlDocument doc, string productName)//Метод получения цены продукта
    {
        string? price = doc.DocumentNode.SelectSingleNode("//span[@class='price_price_kg__Oy2cx']") == null ?
            null : doc.DocumentNode.SelectSingleNode("//span[@class='price_price_kg__Oy2cx']").InnerText.Split(' ')[4];

        if (!string.IsNullOrEmpty(price))
        {
            MatchCollection matches1 = Regex.Matches(price, @"[0-9,]+");
            string productPriceStr = string.Join("", matches1).Trim();
            if (Decimal.TryParse(productPriceStr, out decimal productPrice))
            {
                return productPrice;
            }

        }
        else if (string.IsNullOrEmpty(price))
        {
            string? optPrice = doc.DocumentNode.SelectSingleNode("//span[@class='price_main__nYHyt']") == null ?
                               null : doc.DocumentNode.SelectSingleNode("//span[@class='price_main__nYHyt']").InnerText;


            if (!string.IsNullOrEmpty(optPrice))
            {
                MatchCollection matches2 = Regex.Matches(optPrice, @"[0-9,]+");
                string optProductPriceStr = string.Join("", matches2).Trim();

                if (optPrice.Contains("кг"))
                {
                    if (Decimal.TryParse(optProductPriceStr, out decimal optProductPrice))
                    {
                        return optProductPrice;
                    }
                }

                decimal grammovka = productName.Split(' ')[^2].Replace('.', ',').Contains("х") ?
                       decimal.Parse(productName.Split(' ')[^2].Replace('.', ',').Split("х")[0]) *
                       decimal.Parse(productName.Split(' ')[^2].Replace('.', ',').Split("х")[1]) :
                       decimal.Parse(productName.Split(' ')[^2].Replace('.', ','));

                if (productName.Split(' ')[^1].Replace('.', ',').Equals("г"))
                {
                    if (Decimal.TryParse(optProductPriceStr, out decimal optProductPrice))
                    {
                        return (optProductPrice *1000)/grammovka;
                    }
                }
                else
                {
                    if (Decimal.TryParse(optProductPriceStr, out decimal optProductPrice))
                    {
                        return optProductPrice/grammovka;
                    }
                }
            }
            else
            {
                string? optPriceSkidka = doc.DocumentNode.SelectSingleNode("//span[@class='price_main__nYHyt price_main_red__pOT8N']") == null ?
                                         null : doc.DocumentNode.SelectSingleNode("//span[@class='price_main__nYHyt price_main_red__pOT8N']").InnerText;

                if (!string.IsNullOrEmpty(optPriceSkidka))
                {
                    MatchCollection matches2 = Regex.Matches(optPriceSkidka, @"[0-9,]+");
                    string optProductPriceStr = string.Join("", matches2).Trim();

                    if (optPriceSkidka.Contains("кг"))
                    {
                        if (Decimal.TryParse(optProductPriceStr, out decimal optProductPrice))
                        {
                            return optProductPrice;
                        }
                    }

                    decimal grammovka = productName.Split(' ')[^2].Replace('.', ',').Contains("х") ?
                       decimal.Parse(productName.Split(' ')[^2].Replace('.', ',').Split("х")[0]) *
                       decimal.Parse(productName.Split(' ')[^2].Replace('.', ',').Split("х")[1]) :
                       decimal.Parse(productName.Split(' ')[^2].Replace('.', ','));


                    if (productName.Split(' ')[^1].Replace('.', ',').Equals("г"))
                    {
                        if (Decimal.TryParse(optProductPriceStr, out decimal optProductPrice))
                        {
                            return (optProductPrice *1000)/grammovka;
                        }
                    }
                    else
                    {
                        if (Decimal.TryParse(optProductPriceStr, out decimal optProductPrice))
                        {
                            return optProductPrice/grammovka;
                        }
                    }
                }

            }
        }

        return 0.00m;
    }
    static async Task<string> GetAttributeName(HtmlNode? attribute)//Получение названия характеристики (Оптимизировано)
    {
        try
        {
            var attributeNameNode = attribute!.SelectSingleNode(".//div[@class='table_property__col__3nGyY']");

            if (attributeNameNode != null)
            {
                string attributeName = Regex.Replace(attributeNameNode.InnerText, "[^а-яА-Я0-9,% ]", "");

                if (!string.IsNullOrEmpty(attributeName))
                {
                    return attributeName.Trim();
                }
            }
        }
        catch (Exception ex)
        {
            await LogFile.AddLogMessageAsync("GetAttributeName", ex.Message, mainViewModelInstance, true);
        }

        return "Неизвестно";
    }
    static async Task<string> GetAttributeValue(HtmlNode? attribute)//Получение значения характеристики (Оптимизировано)
    {
        try
        {
            var attributeValueNode = attribute!.SelectSingleNode(".//span[@class='table_property__right__XSjno']");

            if (attributeValueNode != null)
            {
                string attributeValue = attributeValueNode.InnerText;

                if (!string.IsNullOrEmpty(attributeValue))
                {
                    return attributeValue.Trim();
                }
            }

        }
        catch (Exception ex)
        {
            await LogFile.AddLogMessageAsync("GetAttributeValue", ex.Message, mainViewModelInstance, true);
        }

        return "Undefined";
    }
    static async Task<string> GetArticleFromUrl(string url)//Получение артикула продукта (Оптимизировано)
    {
        try
        {
            string[] parts = url.Split('/');
            string lastPart = parts.LastOrDefault()!;

            return lastPart.Trim();
        }
        catch (Exception ex)
        {
            await LogFile.AddLogMessageAsync("GetArticleFromUrl", ex.Message, mainViewModelInstance, true);
        }

        return "Неизвестно";
    }
    #endregion

    static async Task<HtmlNodeCollection> GetAttributes(HtmlNode? attributeContainer)//Получение названий и значений характеристик
    {
        var evroAttributes = attributeContainer.SelectNodes(".//div[@class='table_property__SKkDk']");

        if (evroAttributes != null || !evroAttributes.Any())
        {
            return evroAttributes;
        }
        else
        {
            return null;
        }
    }
    static async Task<HtmlNode> GetAttributeContainer(HtmlDocument doc)//Получение контейнера со всеми характеристиками
    {
        HtmlNode attributeContainer = doc.DocumentNode.SelectSingleNode("//div[@data-id='15']");

        if (attributeContainer != null)
        {
            return attributeContainer;
        }
        else
        {
            return null;
        }
    }
}
