using System.Text.RegularExpressions;

namespace PriceAnalysis.Services.ProductServices;

public class WheatFlourServices
{
    public static string ProcessString(string input)//Получение форматированного названия муки(OP)
    {
        int commaIndex = input.IndexOf(',');

        if (commaIndex != -1)
        {
            string textBetweenQuotes = input.Substring(input.IndexOf('«'), commaIndex - input.IndexOf('«')).Trim();
            return RemoveSortAndPrecedingWord(textBetweenQuotes);
        }
        else
        {
            int quoteIndex = input.IndexOf('«');
            int closingQuoteIndex = input.IndexOf('»', quoteIndex);

            if (quoteIndex != -1 && closingQuoteIndex != -1)
            {
                string textBetweenQuotes = input.Substring(quoteIndex, closingQuoteIndex - quoteIndex + 1).Trim();
                return RemoveSortAndPrecedingWord(textBetweenQuotes);
            }
            else
            {
                return "Совпадений не найдено";
            }
        }
    }

    static string RemoveSortAndPrecedingWord(string input)//Убрать слово сорт и слово перед сорт(OP)
    {
        string pattern = @"(\S*\s*)?сорт(\s*\S*)?";

        Regex regex = new Regex(pattern, RegexOptions.IgnoreCase);

        Match match = regex.Match(input);

        if (match.Success)
        {
            return input.Replace(match.Value, "").Trim();
        }

        return input;
    }

}
