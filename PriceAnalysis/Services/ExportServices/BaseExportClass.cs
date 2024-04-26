using System.Diagnostics;
using System.Globalization;
using System.Windows;

namespace PriceAnalysis.Services.ExportServices;

public class BaseExportClass
{
    //Открытие файла(Оптимизировано)
    protected static void OpenFile(string filePath, string programmPath)
    {
        try
        {
            Process.Start(programmPath, filePath);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
        }
    }

    protected static List<DateTime> GetRequiredDates(params string[] dates)//Получение всех дат, сипользуемых в отчете(Оптимизировано)
    {
        try
        {
            List<DateTime> result = new List<DateTime>();
            for (int i = 0; i < dates.Length; i++)
            {
                var date = dates[i].Trim();
                result.Add(DateTime.ParseExact(date, "dd MMMM yyyy", CultureInfo.CurrentCulture));
            }
            return result;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
        return new List<DateTime>() { DateTime.Now.Date };
    }
}
