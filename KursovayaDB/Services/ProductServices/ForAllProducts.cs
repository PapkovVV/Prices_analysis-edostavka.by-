namespace KursovayaDB.Services.ProductServices;

public class ForAllProducts
{
    public static string GetSubstringBeforeComma(string productName)
    {
        int commaIndex = productName.IndexOf(',');

        if (commaIndex != -1)
        {
            return productName.Substring(0, commaIndex);
        }
        else
        {
            return productName;
        }
    }
}
