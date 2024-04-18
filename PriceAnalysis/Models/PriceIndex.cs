namespace PriceAnalysis.Models;

public class PriceIndex
{
    private static int lastId = 0;

    public PriceIndex()
    {
        Id = ++lastId;
    }

    public int Id { get; private set; } = 0;
    public int CategoryId { get; set; } 
    public decimal IndexValue { get; set; }
    public DateTime IndexDateFrom { get; set; }
    public DateTime IndexDateTo { get; set;}
    public string CategoryName { get; set; }


}
