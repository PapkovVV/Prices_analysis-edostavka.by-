using System.Diagnostics;

namespace PriceAnalysis.Models;

public class Category
{
    private static int lastId = 0;

    public int Id { get; set; } = 0;
    public string Name { get; set; }
    public List<ProductName> products = new List<ProductName>();

    public Category()
    {
        Id = ++lastId;
    }

    public override string ToString()
    {
        string result = string.Join(", ", products.Select(prod => $"{prod}"));
        return $"Категория: {Name}\n{result}";
    }
}
