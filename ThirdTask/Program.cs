using System;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: ThirdTask <input_file> <output_file> [problems_file]");
            Console.WriteLine("Example: ThirdTask input.log output.txt problems.txt");
            return;
        }

        string inputFile = args[0];
        string outputFile = args[1];
        string problemsFile = args.Length > 2 ? args[2] : "problems.txt";

        try
        {
            LogStandardizer.ProcessLogFile(inputFile, outputFile, problemsFile);
            Console.WriteLine($"Обработка завершена. Результат: {outputFile}");
            Console.WriteLine($"Проблемные записи: {problemsFile}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
        }
    }
}