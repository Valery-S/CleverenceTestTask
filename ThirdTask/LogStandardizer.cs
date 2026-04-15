using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

public class LogEntry
{
    public DateTime Date { get; set; }
    public TimeSpan Time { get; set; }
    public string LogLevel { get; set; }
    public string CallingMethod { get; set; }
    public string Message { get; set; }
    public string OriginalLine { get; set; }
    public bool IsValid { get; set; }
}

public static class LogStandardizer
{
    // Регулярные выражения для двух форматов
    private static readonly Regex Format1Regex = new Regex(
        @"^(?<day>\d{2})\.(?<month>\d{2})\.(?<year>\d{4})\s+" +
        @"(?<hour>\d{2}):(?<minute>\d{2}):(?<second>\d{2})\.(?<millisecond>\d+)\s+" +
        @"(?<level>INFORMATION|WARNING|ERROR|DEBUG)\s+" +
        @"(?<message>.+)$"
    );

    private static readonly Regex Format2Regex = new Regex(
        @"^(?<year>\d{4})-(?<month>\d{2})-(?<day>\d{2})\s+" +
        @"(?<hour>\d{2}):(?<minute>\d{2}):(?<second>\d{2})\.(?<millisecond>\d+)\s*\|\s*" +
        @"(?<level>INFO|WARN|ERROR|DEBUG)\s*\|\s*" +
        @"(?<method>[^|]+)\s*\|\s*" +
        @"(?<message>.+)$"
    );

    // Маппинг уровней логирования
    private static readonly Dictionary<string, string> LevelMapping = new Dictionary<string, string>
    {
        { "INFORMATION", "INFO" },
        { "WARNING", "WARN" },
        { "ERROR", "ERROR" },
        { "DEBUG", "DEBUG" },
        { "INFO", "INFO" },
        { "WARN", "WARN" }
    };

    /// <summary>
    /// Основной метод обработки лог-файла
    /// </summary>
    /// <param name="inputFilePath">Путь к входному файлу</param>
    /// <param name="outputFilePath">Путь к выходному файлу со стандартизированными логами</param>
    /// <param name="problemsFilePath">Путь к файлу с проблемными записями (по умолчанию "problems.txt")</param>
    public static void ProcessLogFile(string inputFilePath, string outputFilePath, string problemsFilePath = "problems.txt")
    {
        if (!File.Exists(inputFilePath))
            throw new FileNotFoundException($"Входной файл не найден: {inputFilePath}");

        var validEntries = new List<string>();
        var invalidEntries = new List<string>();

        foreach (var line in File.ReadLines(inputFilePath))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var logEntry = ParseLogLine(line);

            if (logEntry.IsValid)
            {
                validEntries.Add(FormatOutput(logEntry));
            }
            else
            {
                invalidEntries.Add(line); // Сохраняем исходный формат
            }
        }

        // Записываем валидные записи
        File.WriteAllLines(outputFilePath, validEntries);

        // Записываем невалидные записи, если они есть
        if (invalidEntries.Any())
        {
            File.WriteAllLines(problemsFilePath, invalidEntries);
        }
    }

    /// <summary>
    /// Парсинг одной строки лога
    /// </summary>
    public static LogEntry ParseLogLine(string line)
    {
        // Попытка распознать формат 1
        var match1 = Format1Regex.Match(line);
        if (match1.Success)
        {
            return ParseFormat1(match1, line);
        }

        // Попытка распознать формат 2
        var match2 = Format2Regex.Match(line);
        if (match2.Success)
        {
            return ParseFormat2(match2, line);
        }

        // Невалидная запись
        return new LogEntry { IsValid = false, OriginalLine = line };
    }

    /// <summary>
    /// Парсинг формата 1: 10.03.2025 15:14:49.523 INFORMATION Версия программы: '3.4.0.48729'
    /// </summary>
    private static LogEntry ParseFormat1(Match match, string originalLine)
    {
        try
        {
            int day = int.Parse(match.Groups["day"].Value);
            int month = int.Parse(match.Groups["month"].Value);
            int year = int.Parse(match.Groups["year"].Value);

            var date = new DateTime(year, month, day);

            int hour = int.Parse(match.Groups["hour"].Value);
            int minute = int.Parse(match.Groups["minute"].Value);
            int second = int.Parse(match.Groups["second"].Value);
            int millisecond = int.Parse(match.Groups["millisecond"].Value);

            var time = new TimeSpan(0, hour, minute, second, millisecond);

            string inputLevel = match.Groups["level"].Value;
            string outputLevel = MapLogLevel(inputLevel);

            string message = match.Groups["message"].Value.Trim();

            return new LogEntry
            {
                Date = date,
                Time = time,
                LogLevel = outputLevel,
                CallingMethod = "DEFAULT",
                Message = message,
                OriginalLine = originalLine,
                IsValid = true
            };
        }
        catch
        {
            return new LogEntry { IsValid = false, OriginalLine = originalLine };
        }
    }

    /// <summary>
    /// Парсинг формата 2: 2025-03-10 15:14:51.5882| INFO|11|MobileComputer.GetDeviceId| Код устройства: '@MINDEO-M40-D-410244015546'
    /// </summary>
    private static LogEntry ParseFormat2(Match match, string originalLine)
    {
        try
        {
            int year = int.Parse(match.Groups["year"].Value);
            int month = int.Parse(match.Groups["month"].Value);
            int day = int.Parse(match.Groups["day"].Value);

            var date = new DateTime(year, month, day);

            int hour = int.Parse(match.Groups["hour"].Value);
            int minute = int.Parse(match.Groups["minute"].Value);
            int second = int.Parse(match.Groups["second"].Value);
            int millisecond = int.Parse(match.Groups["millisecond"].Value);

            var time = new TimeSpan(0, hour, minute, second, millisecond);

            string inputLevel = match.Groups["level"].Value;
            string outputLevel = MapLogLevel(inputLevel);

            string callingMethod = match.Groups["method"].Value.Trim();

            string message = match.Groups["message"].Value.Trim();

            return new LogEntry
            {
                Date = date,
                Time = time,
                LogLevel = outputLevel,
                CallingMethod = callingMethod,
                Message = message,
                OriginalLine = originalLine,
                IsValid = true
            };
        }
        catch
        {
            return new LogEntry { IsValid = false, OriginalLine = originalLine };
        }
    }

    /// <summary>
    /// Маппинг уровня логирования
    /// </summary>
    private static string MapLogLevel(string inputLevel)
    {
        return LevelMapping.TryGetValue(inputLevel.ToUpper(), out var mapped)
            ? mapped
            : inputLevel;
    }

    /// <summary>
    /// Форматирование выходной записи
    /// Формат: Дата\tВремя\tУровеньЛогирования\tВызвавшийМетод\tСообщение
    /// </summary>
    public static string FormatOutput(LogEntry entry)
    {
        // Дата в формате DD-MM-YYYY
        string formattedDate = entry.Date.ToString("dd-MM-yyyy");

        // Время в исходном формате (часы:минуты:секунды.миллисекунды)
        string formattedTime = entry.Time.ToString(@"hh\:mm\:ss\.fff");

        // Удаляем лишние точки в миллисекундах (если их больше 3)
        if (formattedTime.Length > 12)
        {
            formattedTime = formattedTime.Substring(0, 12);
        }

        // Формируем строку с табуляцией в качестве разделителя
        return $"{formattedDate}\t{formattedTime}\t{entry.LogLevel}\t{entry.CallingMethod}\t{entry.Message}";
    }
}