using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

public static class ServerForTests
{
    private static int _count = 0;
    private static readonly ReaderWriterLockSlim _rwLock = new ReaderWriterLockSlim();

    // Для статистики и отладки
    private static int _activeReaders = 0;
    private static int _activeWriters = 0;
    private static readonly object _statsLock = new object();

    // Флаг для тестового режима
    private static bool _testMode = false;
    private static int _testDelayMs = 0;

    // ========== ОСНОВНЫЕ МЕТОДЫ ==========

    public static int GetCount()
    {
        _rwLock.EnterReadLock();
        try
        {
            // Регистрируем активного читателя (для тестов)
            IncrementActiveReaders();

            // Имитация небольшой работы (для тестов)
            if (_testMode && _testDelayMs > 0)
            {
                Thread.Sleep(_testDelayMs);
            }

            return _count;
        }
        finally
        {
            DecrementActiveReaders();
            _rwLock.ExitReadLock();
        }
    }

    public static void AddToCount(int value)
    {
        _rwLock.EnterWriteLock();
        try
        {
            // Регистрируем активного писателя (для тестов)
            IncrementActiveWriters();

            // Имитация работы писателя (для тестов)
            Thread.Sleep(5);

            _count += value;
        }
        finally
        {
            DecrementActiveWriters();
            _rwLock.ExitWriteLock();
        }
    }

    // ========== МЕТОДЫ ДЛЯ ТЕСТИРОВАНИЯ ==========

    /// <summary>
    /// Сброс счётчика в ноль (с блокировкой)
    /// </summary>
    public static void Reset()
    {
        _rwLock.EnterWriteLock();
        try
        {
            _count = 0;
            _activeReaders = 0;
            _activeWriters = 0;
        }
        finally
        {
            _rwLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Получить текущее значение без блокировки (ТОЛЬКО ДЛЯ ТЕСТОВ!)
    /// </summary>
    public static int GetCountUnsafe()
    {
        return _count;
    }

    /// <summary>
    /// Получить количество активных читателей (для проверки параллельности)
    /// </summary>
    public static int GetActiveReadersCount()
    {
        lock (_statsLock)
        {
            return _activeReaders;
        }
    }

    /// <summary>
    /// Получить количество активных писателей (всегда 0 или 1)
    /// </summary>
    public static int GetActiveWritersCount()
    {
        lock (_statsLock)
        {
            return _activeWriters;
        }
    }

    /// <summary>
    /// Включить задержку при чтениии для нагрузочного теста
    /// </summary>
    public static void EnableTestMode(int delayMs = 10)
    {
        _testMode = true;
        _testDelayMs = delayMs;
    }

    /// <summary>
    /// Выключить задержку при чтениии для нагрузочного теста
    /// </summary>
    public static void DisableTestMode()
    {
        _testMode = false;
    }

    // ========== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ДЛЯ СТАТИСТИКИ ==========

    private static void IncrementActiveReaders()
    {
        lock (_statsLock)
        {
            _activeReaders++;
        }
    }

    private static void DecrementActiveReaders()
    {
        lock (_statsLock)
        {
            _activeReaders--;
        }
    }

    private static void IncrementActiveWriters()
    {
        lock (_statsLock)
        {
            _activeWriters++;
        }
    }

    private static void DecrementActiveWriters()
    {
        lock (_statsLock)
        {
            _activeWriters--;
        }
    }

    // ========== МЕТОДЫ ДЛЯ НАГРУЗОЧНОГО ТЕСТИРОВАНИЯ ==========

    /// <summary>
    /// Запустить N читателей, которые будут читать счётчик
    /// </summary>
    public static async Task RunMultipleReaders(int readerCount, int readsPerReader, CancellationToken cancellationToken = default)
    {
        var tasks = new List<Task>();
        for (int i = 0; i < readerCount; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                for (int j = 0; j < readsPerReader; j++)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    int value = GetCount();
                    await Task.Delay(1, cancellationToken); // небольшая задержка между чтениями
                }
            }, cancellationToken));
        }
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Запустить N писателей, каждый добавит свою сумму
    /// </summary>
    public static async Task RunMultipleWriters(int writerCount, int valuePerWriter, CancellationToken cancellationToken = default)
    {
        var tasks = new List<Task>();
        for (int i = 0; i < writerCount; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < valuePerWriter; j++)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    AddToCount(1);
                }
            }, cancellationToken));
        }
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Запустить смешанную нагрузку: читатели и писатели одновременно
    /// </summary>
    public static async Task<(int finalValue, TimeSpan executionTime)> RunMixedLoad(
        int readerCount,
        int writerCount,
        int operationsPerWorker,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var tasks = new List<Task>();

        // Запускаем читателей
        for (int i = 0; i < readerCount; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                for (int j = 0; j < operationsPerWorker; j++)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    GetCount();
                    await Task.Delay(1, cancellationToken);
                }
            }, cancellationToken));
        }

        // Запускаем писателей
        for (int i = 0; i < writerCount; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < operationsPerWorker; j++)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    AddToCount(1);
                }
            }, cancellationToken));
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        return (GetCount(), stopwatch.Elapsed);
    }

    /// <summary>
    /// Проверить, что читатели действительно работают параллельно
    /// Возвращает максимальное количество одновременно активных читателей
    /// </summary>
    public static async Task<int> TestReadersParallelism(int readerCount, int durationMs = 100)
    {
        Reset();
        var maxReaders = 0;
        var cts = new CancellationTokenSource(durationMs);

        var tasks = new List<Task>();
        for (int i = 0; i < readerCount; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    GetCount(); // вызовет IncrementActiveReaders внутри
                    await Task.Delay(1);

                    // Обновляем максимум
                    var current = GetActiveReadersCount();
                    if (current > maxReaders)
                    {
                        Interlocked.Exchange(ref maxReaders, current);
                    }
                }
            }, cts.Token));
        }

        await Task.WhenAll(tasks);
        return maxReaders;
    }

    /// <summary>
    /// Проверить, что писатели не работают одновременно
    /// Возвращает максимальное количество одновременно активных писателей (должно быть 1)
    /// </summary>
    public static async Task<int> TestWritersSequential(int writerCount, int durationMs = 100)
    {
        Reset();
        var maxWriters = 0;
        var cts = new CancellationTokenSource(durationMs);

        var tasks = new List<Task>();
        for (int i = 0; i < writerCount; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    AddToCount(1); // вызовет IncrementActiveWriters внутри
                    await Task.Delay(2);

                    var current = GetActiveWritersCount();
                    if (current > maxWriters)
                    {
                        Interlocked.Exchange(ref maxWriters, current);
                    }
                }
            }, cts.Token));
        }

        await Task.WhenAll(tasks);
        return maxWriters;
    }

    /// <summary>
    /// Проверить, что читатели ждут писателей
    /// </summary>
    public static async Task<bool> TestReadersWaitForWriters()
    {
        Reset();
        var readerStartedDuringWrite = false;
        var writeLockHeld = false;

        // Запускаем писателя с длительной операцией
        var writerTask = Task.Run(() =>
        {
            _rwLock.EnterWriteLock();
            try
            {
                writeLockHeld = true;
                Thread.Sleep(100); // держим блокировку 100 мс
            }
            finally
            {
                writeLockHeld = false;
                _rwLock.ExitWriteLock();
            }
        });

        // Даём писателю время захватить блокировку
        await Task.Delay(10);

        // Пытаемся прочитать
        var readerTask = Task.Run(() =>
        {
            var stopwatch = Stopwatch.StartNew();
            GetCount();
            stopwatch.Stop();

            // Если читатель ждал больше 50 мс, значит он ждал писателя
            readerStartedDuringWrite = stopwatch.ElapsedMilliseconds > 50;
        });

        await Task.WhenAll(writerTask, readerTask);

        return readerStartedDuringWrite && !writeLockHeld;
    }
}