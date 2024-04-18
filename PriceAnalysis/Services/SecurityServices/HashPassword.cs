using System.Collections;
using System.Security.Cryptography;
using System.Text;

namespace KursovayaDB.Services.SecurityServices;

public class HashPassword
{
    public static string Hash_Password(string password)//Преобразование строки пароля в Хэш-пароль
    {
        using (var sha256 = SHA256.Create())
        {
            // Преобразование пароля в байты
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

            // Вычисление хеша
            byte[] hashBytes = sha256.ComputeHash(passwordBytes);

            // Преобразование хеша в строку Base64
            return Convert.ToBase64String(hashBytes);
        }
    }
    public static bool VerifyPassword(string password, string hashedPassword)//Сравнение введенного пароля с имеющимся Хеш-паролем
    {
        // Получение байтов из хеша
        byte[] storedHashBytes = Convert.FromBase64String(hashedPassword);

        using (var sha256 = SHA256.Create())
        {
            // Преобразование введенного пароля в байты
            byte[] enteredPasswordBytes = Encoding.UTF8.GetBytes(password);

            // Вычисление хеша для введенного пароля
            byte[] enteredHashBytes = sha256.ComputeHash(enteredPasswordBytes);

            // Сравнение двух хешей
            return StructuralComparisons.StructuralEqualityComparer.Equals(storedHashBytes, enteredHashBytes);
        }
    }
}
