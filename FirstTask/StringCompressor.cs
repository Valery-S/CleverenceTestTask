using System;
using System.Text;
using System.Text.RegularExpressions;

public class StringCompressor
{
    /// <summary>
    /// Сжимает строку по правилу RLE: группа одинаковых символов заменяется на "символ+количество",
    /// если количество > 1. Если количество == 1, пишется только символ.
    /// </summary>
    /// <param name="input">Исходная строка из маленьких латинских букв</param>
    /// <returns>Сжатая строка</returns>
    public static string Compress(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var result = new StringBuilder();
        int count = 1;

        for (int i = 1; i <= input.Length; i++)
        {
            // Если текущий символ совпадает с предыдущим и мы не в конце строки
            if (i < input.Length && input[i] == input[i - 1])
            {
                count++;
            }
            else
            {
                // Записываем предыдущую группу
                result.Append(input[i - 1]);
                if (count > 1)
                    result.Append(count);

                // Сбрасываем счётчик для новой группы
                count = 1;
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// Восстанавливает исходную строку из сжатой.
    /// Формат: буква, за которой может следовать число (количество повторений).
    /// Если числа нет — значит буква одна.
    /// </summary>
    /// <param name="compressed">Сжатая строка</param>
    /// <returns>Исходная строка</returns>
    public static string Decompress(string compressed)
    {
        if (string.IsNullOrEmpty(compressed))
            return compressed;

        var result = new StringBuilder();

        // Регулярное выражение: буква, за которой может следовать число (одна или более цифр)
        MatchCollection matches = Regex.Matches(compressed, @"([a-z])(\d+)?");

        foreach (Match match in matches)
        {
            char ch = match.Groups[1].Value[0];
            string countStr = match.Groups[2].Value;

            int count = string.IsNullOrEmpty(countStr) ? 1 : int.Parse(countStr);

            result.Append(ch, count);
        }

        return result.ToString();
    }
}