using PriceAnalysis.ViewModel;
using System.IO;

namespace PriceAnalysis.Services.LogServices;

public class LogFile
{
    private static readonly string logFilePath = "LogData.txt";

    private static readonly Mutex logMutex = new Mutex();//Блокировщик для распределения доступа к файлу из разных потоков

    public static async Task Create() //Создание Log-файла (Оптимизировано)
    {
        if (!File.Exists(logFilePath))
        {
            File.Create(logFilePath);
        }
    }

    public static async Task WriteMessage(string methodName, string message, bool isError) //Запись в Log-файл(Оптимизировано)
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

    public static async Task AddLogMessageAsync(string methodName, string message, MainViewModel viewModel = null, bool isError = false)//Добавление Log-информации для вывода(Оптимизировано)
    {
        if (viewModel != null)
        {
            viewModel.LogText += $"{message}\n";
        }

        logMutex.WaitOne();

        try
        {
            await WriteMessage(methodName, message, isError);
        }
        finally
        {
            logMutex.ReleaseMutex();
        }
    }
}
