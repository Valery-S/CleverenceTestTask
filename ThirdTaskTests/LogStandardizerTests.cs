using Xunit;
using System;
using System.IO;
using System.Linq;

public class LogStandardizerTests
{
    private const string TestInputFile = "test_input.log";
    private const string TestOutputFile = "test_output.txt";
    private const string TestProblemsFile = "test_problems.txt";

    private void CleanupTestFiles()
    {
        if (File.Exists(TestInputFile)) File.Delete(TestInputFile);
        if (File.Exists(TestOutputFile)) File.Delete(TestOutputFile);
        if (File.Exists(TestProblemsFile)) File.Delete(TestProblemsFile);
    }

    // ========== ТЕСТЫ ПАРСИНГА ФОРМАТА 1 ==========

    [Fact]
    public void ParseFormat1_ValidInformationLog_ReturnsCorrectEntry()
    {
        // Arrange
        string line = "10.03.2025 15:14:49.523 INFORMATION Версия программы: '3.4.0.48729'";

        // Act
        var result = LogStandardizer.ParseLogLine(line);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(new DateTime(2025, 3, 10), result.Date);
        Assert.Equal(15, result.Time.Hours);
        Assert.Equal(14, result.Time.Minutes);
        Assert.Equal(49, result.Time.Seconds);
        Assert.Equal(523, result.Time.Milliseconds);
        Assert.Equal("INFO", result.LogLevel);
        Assert.Equal("DEFAULT", result.CallingMethod);
        Assert.Equal("Версия программы: '3.4.0.48729'", result.Message);
    }

    [Fact]
    public void ParseFormat1_ValidWarningLog_ReturnsCorrectEntry()
    {
        // Arrange
        string line = "15.07.2024 10:30:22.123 WARNING Предупреждение: низкий заряд батареи";

        // Act
        var result = LogStandardizer.ParseLogLine(line);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(new DateTime(2024, 7, 15), result.Date);
        Assert.Equal("WARN", result.LogLevel);
        Assert.Equal("DEFAULT", result.CallingMethod);
        Assert.Equal("Предупреждение: низкий заряд батареи", result.Message);
    }

    [Fact]
    public void ParseFormat1_ValidErrorLog_ReturnsCorrectEntry()
    {
        // Arrange
        string line = "01.01.2025 00:00:01.999 ERROR Критическая ошибка";

        // Act
        var result = LogStandardizer.ParseLogLine(line);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(new DateTime(2025, 1, 1), result.Date);
        Assert.Equal("ERROR", result.LogLevel);
        Assert.Equal("Критическая ошибка", result.Message);
    }

    [Fact]
    public void ParseFormat1_ValidDebugLog_ReturnsCorrectEntry()
    {
        // Arrange
        string line = "20.12.2024 23:59:59.001 DEBUG Отладочная информация";

        // Act
        var result = LogStandardizer.ParseLogLine(line);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal("DEBUG", result.LogLevel);
    }

    // ========== ТЕСТЫ ПАРСИНГА ФОРМАТА 2 ==========

    [Fact]
    public void ParseFormat2_ValidInfoLog_ReturnsCorrectEntry()
    {
        // Arrange
        string line = "2025-03-10 15:14:51.882| INFO|11|MobileComputer.GetDeviceId| Код устройства: '@MINDEO-M40-D-410244015546'";

        // Act
        var result = LogStandardizer.ParseLogLine(line);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(new DateTime(2025, 3, 10), result.Date);
        Assert.Equal(15, result.Time.Hours);
        Assert.Equal(14, result.Time.Minutes);
        Assert.Equal(51, result.Time.Seconds);
        Assert.Equal(882, result.Time.Milliseconds);
        Assert.Equal("INFO", result.LogLevel);
        Assert.Equal("MobileComputer.GetDeviceId", result.CallingMethod);
        Assert.Equal("Код устройства: '@MINDEO-M40-D-410244015546'", result.Message);
    }

    [Fact]
    public void ParseFormat2_ValidWarnLog_ReturnsCorrectEntry()
    {
        // Arrange
        string line = "2024-12-01 08:15:30.123| WARN|5|Logger.Write| Предупреждение: таймаут операции";

        // Act
        var result = LogStandardizer.ParseLogLine(line);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal("WARN", result.LogLevel);
        Assert.Equal("Logger.Write", result.CallingMethod);
    }

    [Fact]
    public void ParseFormat2_WithDifferentMillisecondLength_HandlesCorrectly()
    {
        // Arrange
        string line = "2024-12-01 08:15:30.1| INFO|5|TestMethod| Сообщение";

        // Act
        var result = LogStandardizer.ParseLogLine(line);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(1, result.Time.Milliseconds);
    }

    // ========== ТЕСТЫ НЕВАЛИДНЫХ ЗАПИСЕЙ ==========

    [Fact]
    public void ParseLogLine_InvalidFormat_ReturnsInvalidEntry()
    {
        // Arrange
        string line = "Это просто текст, не лог";

        // Act
        var result = LogStandardizer.ParseLogLine(line);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(line, result.OriginalLine);
    }

    [Fact]
    public void ParseLogLine_EmptyLine_ReturnsInvalidEntry()
    {
        // Arrange
        string line = "";

        // Act
        var result = LogStandardizer.ParseLogLine(line);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public void ParseLogLine_NullLine_ReturnsInvalidEntry()
    {
        // Act
        var result = LogStandardizer.ParseLogLine(null);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public void ParseLogLine_PartiallyCorruptedFormat1_ReturnsInvalidEntry()
    {
        // Arrange
        string line = "10.03.2025 15:14:49.523 UNKNOWN LEVEL Сообщение";

        // Act
        var result = LogStandardizer.ParseLogLine(line);

        // Assert
        Assert.False(result.IsValid);
    }

    // ========== ТЕСТЫ ФОРМАТИРОВАНИЯ ВЫВОДА ==========

    [Fact]
    public void FormatOutput_ValidEntry_ReturnsCorrectFormat()
    {
        // Arrange
        var entry = new LogEntry
        {
            Date = new DateTime(2025, 3, 10),
            Time = new TimeSpan(0,15, 14, 49, 523),
            LogLevel = "INFO",
            CallingMethod = "DEFAULT",
            Message = "Версия программы: '3.4.0.48729'",
            IsValid = true
        };

        // Act
        string result = LogStandardizer.FormatOutput(entry);

        // Assert
        Assert.Equal("10-03-2025\t15:14:49.523\tINFO\tDEFAULT\tВерсия программы: '3.4.0.48729'", result);
    }

    [Fact]
    public void FormatOutput_WithCustomMethod_ReturnsCorrectFormat()
    {
        // Arrange
        var entry = new LogEntry
        {
            Date = new DateTime(2025, 3, 10),
            Time = new TimeSpan(0,15, 14, 51, 588),
            LogLevel = "INFO",
            CallingMethod = "MobileComputer.GetDeviceId",
            Message = "Код устройства: '@MINDEO-M40-D-410244015546'",
            IsValid = true
        };

        // Act
        string result = LogStandardizer.FormatOutput(entry);

        // Assert
        Assert.Equal("10-03-2025\t15:14:51.588\tINFO\tMobileComputer.GetDeviceId\tКод устройства: '@MINDEO-M40-D-410244015546'", result);
    }

    // ========== ТЕСТЫ МАППИНГА УРОВНЕЙ ==========

    [Theory]
    [InlineData("INFORMATION", "INFO")]
    [InlineData("WARNING", "WARN")]
    [InlineData("ERROR", "ERROR")]
    [InlineData("DEBUG", "DEBUG")]
    public void LogLevelMapping_CorrectlyMaps(string input, string expected)
    {
        // Arrange & Act
        string line = $"2024-01-01 00:00:00.000 {input} Тестовое сообщение";

        // Для формата 1
        string format1Line = $"01.01.2024 00:00:00.000 {input} Тестовое сообщение";

        // Act
        var result = LogStandardizer.ParseLogLine(format1Line);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(expected, result.LogLevel);
    }

    // ========== ИНТЕГРАЦИОННЫЕ ТЕСТЫ ==========

    [Fact]
    public void ProcessLogFile_MixedValidAndInvalidEntries_CreatesCorrectOutputs()
    {
        try
        {
            // Arrange
            CleanupTestFiles();

            var inputLines = new[]
            {
                "10.03.2025 15:14:49.523 INFORMATION Версия программы: '3.4.0.48729'",
                "Некорректная строка лога",
                "2025-03-10 15:14:51.882| INFO|11|MobileComputer.GetDeviceId| Код устройства: '@MINDEO-M40-D-410244015546'",
                "Ещё одна невалидная запись"
            };
            File.WriteAllLines(TestInputFile, inputLines);

            // Act
            LogStandardizer.ProcessLogFile(TestInputFile, TestOutputFile, TestProblemsFile);

            // Assert
            var outputLines = File.ReadAllLines(TestOutputFile);
            var problemLines = File.ReadAllLines(TestProblemsFile);

            Assert.Equal(2, outputLines.Length);
            Assert.Equal(2, problemLines.Length);

            Assert.Contains("10-03-2025", outputLines[0]);
            Assert.Contains("INFO", outputLines[0]);
            Assert.Contains("DEFAULT", outputLines[0]);

            Assert.Contains("10-03-2025", outputLines[1]);
            Assert.Contains("MobileComputer.GetDeviceId", outputLines[1]);

            Assert.Equal("Некорректная строка лога", problemLines[0]);
            Assert.Equal("Ещё одна невалидная запись", problemLines[1]);
        }
        finally
        {
            CleanupTestFiles();
        }
    }

    [Fact]
    public void ProcessLogFile_OnlyValidEntries_NoProblemsFileCreated()
    {
        try
        {
            // Arrange
            CleanupTestFiles();

            var inputLines = new[]
            {
                "10.03.2025 15:14:49.523 INFORMATION Версия программы: '3.4.0.48729'",
                "2025-03-10 15:14:51.882| INFO|11|MobileComputer.GetDeviceId| Код устройства: '@MINDEO-M40-D-410244015546'"
            };
            File.WriteAllLines(TestInputFile, inputLines);

            // Act
            LogStandardizer.ProcessLogFile(TestInputFile, TestOutputFile, TestProblemsFile);

            // Assert
            Assert.True(File.Exists(TestOutputFile));
            Assert.False(File.Exists(TestProblemsFile));

            var outputLines = File.ReadAllLines(TestOutputFile);
            Assert.Equal(2, outputLines.Length);
        }
        finally
        {
            CleanupTestFiles();
        }
    }

    [Fact]
    public void ProcessLogFile_OnlyInvalidEntries_OutputFileContainsNothing()
    {
        try
        {
            // Arrange
            CleanupTestFiles();

            var inputLines = new[]
            {
                "Некорректная строка 1",
                "Некорректная строка 2",
                "Совсем не похоже на лог"
            };
            File.WriteAllLines(TestInputFile, inputLines);

            // Act
            LogStandardizer.ProcessLogFile(TestInputFile, TestOutputFile, TestProblemsFile);

            // Assert
            Assert.True(File.Exists(TestOutputFile));
            Assert.True(File.Exists(TestProblemsFile));

            var outputLines = File.ReadAllLines(TestOutputFile);
            var problemLines = File.ReadAllLines(TestProblemsFile);

            Assert.Empty(outputLines);
            Assert.Equal(3, problemLines.Length);
        }
        finally
        {
            CleanupTestFiles();
        }
    }

    [Fact]
    public void ProcessLogFile_EmptyFile_CreatesEmptyOutputs()
    {
        try
        {
            // Arrange
            CleanupTestFiles();
            File.WriteAllText(TestInputFile, "");

            // Act
            LogStandardizer.ProcessLogFile(TestInputFile, TestOutputFile, TestProblemsFile);

            // Assert
            Assert.True(File.Exists(TestOutputFile));
            Assert.False(File.Exists(TestProblemsFile));

            var outputLines = File.ReadAllLines(TestOutputFile);
            Assert.Empty(outputLines);
        }
        finally
        {
            CleanupTestFiles();
        }
    }

    [Fact]
    public void ProcessLogFile_FileNotFound_ThrowsException()
    {
        // Arrange
        CleanupTestFiles();
        string nonExistentFile = "non_existent_file_12345.log";

        // Act & Assert
        Assert.Throws<FileNotFoundException>(() =>
            LogStandardizer.ProcessLogFile(nonExistentFile, TestOutputFile, TestProblemsFile));
    }
}