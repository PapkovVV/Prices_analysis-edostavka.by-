namespace PriceAnalysis.Models;

public class ProductPrice
{
    private static int lastId = 0;
    public int Id { get; private set; } = 0;
    public string ProductId { get; set; }
    public decimal Price { get; set; }
    public DateTime PriceDate { get; set; }
    public ProductPrice()
    {
        Id = ++lastId;
    }

}
