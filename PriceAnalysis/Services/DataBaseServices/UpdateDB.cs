using KursovayaDB.DataBaseServices;
using KursovayaDB.Models;
using KursovayaDB.Services.ParsingServices;
using KursovayaDB.Services.ProductServices;
using System.Windows;

namespace KursovayaDB.Services.DataBaseServices;

public static class UpdateDB
{
    public static async Task UpdateDataBase()//Обновление значений БД(OP)
    {
        string result = string.Empty;

        IEnumerable<Category> allCategories = Parsing.categories;//Все категории
        IEnumerable<ProductName> allProducts = allCategories.SelectMany(x => x.products).ToList();//Все продукты
        IEnumerable<ProductPrice> allProductPrices = allProducts.Select(x => x.ProductPrice).ToList();//Все цены на продукты
        IEnumerable<ProductAttribute> allAttributes = Parsing.productAttributes;//Все названия характеристик товаров
        IEnumerable<AttributeValues> allAttributeValues = Parsing.attributeValues;//Все значения характеристик

        int addedCatCount = await UpdateProductCategories(allCategories);
        int addedProductCount = await UpdateProducts(allProducts);
        int addedProductPricesCount = await UpdateProductPrices(allProductPrices);
        int addedAttributeNamesCount = await UpdateAttributeNames(allAttributes);
        int addedAttributeValuesCount = await UpdateAttributeValues(allAttributeValues);
        int addedAveragePricesCount = await UpdateAveragePrices(allCategories);
        int addecPriceIndexesCount = await UpdatePriceIndexes(allCategories);


        if (addedCatCount > 0) result += $"Количество добавленных категорий: {addedCatCount}\n";
        if (addedProductCount > 0) result += $"Количество добавленных продуктов: {addedProductCount}\n";
        if (addedProductPricesCount > 0) result += $"Количество добавленных цен: {addedProductPricesCount}\n";
        if (addedAttributeNamesCount > 0) result += $"Количество добавленных названий характеристик: {addedAttributeNamesCount}\n";
        if (addedAttributeValuesCount > 0) result += $"Количество добавленных значений характеристик: {addedAttributeValuesCount}\n";
        if (addedAveragePricesCount > 0) result += $"Количество добавленных значений средних цен: {addedAveragePricesCount}\n";
        if (addecPriceIndexesCount > 0) result += $"Количество добавленных значений индексов цен: {addecPriceIndexesCount}\n";
        if (!string.IsNullOrEmpty(result)) MessageBox.Show(result);
    }

    static async Task<int> UpdateProductCategories(IEnumerable<Category> allCategories)//Добавление категорий продуктов в БД(OP)
    {
        List<Category> addedCategories = new List<Category>();//Категории, добавленные в БД

        foreach (Category category in allCategories)
        {
            if (await SQLScripts.AddCategories(category.Id ,category.Name) == 1)
            {
                addedCategories.Add(category);
            }
        }

        return addedCategories.Count;
    }

    static async Task<int> UpdateProducts(IEnumerable<ProductName> allProducts)//Добавление продуктов в БД(OP)
    {
        List<ProductName> addedProducts = new List<ProductName>();//Продукты, добавленные в БД

        int result;

        string productName = string.Empty;

        foreach (var product in allProducts)
        {
            if (product.CategoryId.Equals(1))
            {
                productName = WheatFlourServices.ProcessString(product.Name);
            }
            else
            {
                productName = ForAllProducts.GetSubstringBeforeComma(product.Name);
            }

            result = await SQLScripts.AddProductName(product.Article, product.CategoryId, productName);
            if (result == 1)
            {
                addedProducts.Add(new ProductName { CategoryId = product.CategoryId, Name =  productName, ProductPrice = product.ProductPrice });
            }
        }

        return addedProducts.Count;
    }

    static async Task<int> UpdateProductPrices(IEnumerable<ProductPrice> allProductPrices)//Добавление цен продуктов в БД(OP)
    {
        List<ProductPrice> addedProductPrices = new List<ProductPrice>();//Цены, добавленные в БД
        foreach (var productPrice in allProductPrices)
        {
            if (await SQLScripts.AddProductPrice(productPrice.ProductId, productPrice.Price) == 1)
            {
                addedProductPrices.Add(productPrice);
            }
        }

        return addedProductPrices.Count;
    }

    static async Task<int> UpdateAttributeNames(IEnumerable<ProductAttribute> allAttributes)//Добавление названий характеристик в БД(OP)
    {
        List<ProductAttribute> addedProductAttributes = new List<ProductAttribute>();
        foreach (var attribute in allAttributes)
        {
            if (await SQLScripts.AddAttributeName(attribute.Name.Trim()) == 1)
            {
                addedProductAttributes.Add(attribute);
            }
        }
        return addedProductAttributes.Count;
    }

    static async Task<int> UpdateAttributeValues(IEnumerable<AttributeValues> attributeValues)//Добавление значений хаарктеристик в БД(OP)
    {
        List<AttributeValues> addedAttributeValue = new List<AttributeValues>();

        foreach (var attributeValue in attributeValues)
        {
            if (await SQLScripts.AddAttributeValue(attributeValue.ProductId, attributeValue.AttributeName, attributeValue.Value) == 1)
            {
                addedAttributeValue.Add(attributeValue);
            }
        }
        return addedAttributeValue.Count;
    }

    static async Task<int> UpdateAveragePrices(IEnumerable<Category> allCategories)//Добавление значений средних цен продуктов категории(OP)
    {
        int countRows = 0;
        foreach (var category in allCategories)
        {
            if (await SQLScripts.CalculateAveragePrices(category.Id) == 1)
            {
                countRows++;
            }
        }
        return countRows;
    }
    static async Task<int> UpdatePriceIndexes(IEnumerable<Category> allCategories)//Добавление значений индексов потребительских цен продуктов категории(OP)
    {
        int countRows = 0;
        foreach (var category in allCategories)
        {
            if (await SQLScripts.CalculatePriceIndexes(category.Id) == 1)
            {
                countRows++;
            }
        }
        return countRows;
    }

}
