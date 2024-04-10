namespace KursovayaDB.Models;

public class AttributeValues
{
    private static int lastId = 0;

    public int Id { get; private set; } = 0;
    public string ProductId { get; set; }
    public int AttributeId { get; set; }
    public string Value { get; set; } = string.Empty;
    public string AttributeName { get; set; }

    public AttributeValues()
    {
        Id = ++lastId;
    }

    public override string ToString()
    {
        return $"ProductId:{ProductId}, AttributeId:{AttributeId}, Value:{Value}\n";
    }
}
