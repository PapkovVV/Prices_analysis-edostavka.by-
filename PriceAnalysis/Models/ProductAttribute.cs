namespace PriceAnalysis.Models;

public class ProductAttribute
{
    private static int lastId = 0;

    public int Id { get; set; } = 0;
    public string Name { get; set; } = null!; //Название характеристики продукта
    public List <AttributeValues> attributeValues { get; set; } = new List<AttributeValues>(); //Список значений характеристик продукта

    public ProductAttribute()
    {
        Id = ++lastId;
    }
}
