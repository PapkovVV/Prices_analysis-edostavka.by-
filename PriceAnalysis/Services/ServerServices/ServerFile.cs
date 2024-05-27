using System.IO;
using System.Reflection;

namespace PriceAnalysis.Services.ServerServices;

public class ServerFile
{
    private static readonly string serverFilePath = "SavedServer.txt";

    private static void CreateFile()//Создание файла сервера MS SQL(OP)
    {
        if (!File.Exists(serverFilePath))
        {
            using (FileStream fs = File.Create(serverFilePath)) { }
        }
    }

    public static void SaveServerName(string serverName)//Запись в файл имени сервера(OP)
    {
        CreateFile();

        FileInfo fileInfo = new FileInfo(serverFilePath);
        fileInfo.IsReadOnly = false;

        using (StreamWriter writer = new StreamWriter(serverFilePath, false))
        {
            writer.WriteLine(serverName);
        }

        fileInfo.IsReadOnly = true;
    }

    public static string GetServerName()//Получение имени сервера для работы с БД(OP)
    {
        using (StreamReader reader = new StreamReader(serverFilePath, true))
        {
            return reader.ReadLine()!;
        }
    }

    public static bool IsEmptyServerFile()//Проверка на пустоту или несуществование файла(OP)
    {
        if (!File.Exists(serverFilePath))
        {
            return true; 
        }

        using (StreamReader reader = new StreamReader(serverFilePath, true))
        {
            if ( reader.Peek() == -1)
            {
                return true;
            }
            return false;
        }
    }
}
