using System.IO;

namespace KursovayaDB.Services.LogServices;

public class LogFile
{
    const string logFilePath = "LogData.txt";
    public static async Task Create() //Создание Log-файла (Оптимизировано)
    {
        if (!File.Exists(logFilePath))
        {
            File.Create(logFilePath);
        }
    }

    public static async Task WriteMessage(string methodName, string message, bool isError) //Запись в Log-файл
    {
        using (StreamWriter writer = new StreamWriter(logFilePath, true))
        {
            if (isError)
            {
                await writer.WriteLineAsync($"[ERROR] {DateTime.Now} {methodName}: {message}");
            }
            else
            {
                await writer.WriteLineAsync($"{DateTime.Now} {methodName}: {message}");
            }
        }
    }
}
