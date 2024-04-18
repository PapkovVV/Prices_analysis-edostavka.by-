namespace KursovayaDB.Models;

public class ProductName
{
    private static int lastId = 0;

    public int Id { get; private set; } = 0;
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Article { get; set; } = string.Empty;
    public ProductPrice ProductPrice { get; set; } = null!;


    public List<ProductAttribute> attributes = new List<ProductAttribute>();
    public List<ProductPrice> prices = new List<ProductPrice>();


    public ProductName()
    {
        Id = ++lastId;
    }
}
