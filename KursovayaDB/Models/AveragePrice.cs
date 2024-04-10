using KursovayaDB.DataBaseServices;

namespace KursovayaDB.Models;

public class AveragePrice
{
    private static int lastId = 0;

    public AveragePrice()
    {
        Id = ++lastId;
    }

    public int Id { get; private set; } = 0;
    public int CategoryId { get; set; }
    public decimal Average_Price { get; set; }
    public DateTime AveragePriceDate { get; set; }
    public string CategoryName { get; set; } = null!;

}
