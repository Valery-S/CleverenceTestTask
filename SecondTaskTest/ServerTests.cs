using Xunit;
using System.Threading.Tasks;

public class ServerTests
{
    [Fact]
    public async Task Test_ReadersWorkInParallel()
    {
        ServerForTests.Reset();
        ServerForTests.EnableTestMode(delayMs: 10);

        // Запускаем 10 читателей и проверяем, сколько работают одновременно
        int maxParallelReaders = await ServerForTests.TestReadersParallelism(10, 200);

        // Должно быть больше 1 (значит работали параллельно)
        Assert.True(maxParallelReaders > 1, $"Максимум параллельных читателей: {maxParallelReaders}");
    }

    [Fact]
    public async Task Test_WritersWorkSequentially()
    {
        ServerForTests.Reset();

        // Запускаем 5 писателей
        int maxParallelWriters = await ServerForTests.TestWritersSequential(5, 200);

        // Должно быть не больше 1
        Assert.True(maxParallelWriters <= 1, $"максимум параллельных писателей: {maxParallelWriters}");
    }

    [Fact]
    public async Task Test_ReadersWaitForWriters()
    {
        bool readersWaited = await ServerForTests.TestReadersWaitForWriters();
        Assert.True(readersWaited, "читатели должны ждать окончания записи");
    }

    [Fact]
    public async Task Test_MixedLoad_Correctness()
    {
        ServerForTests.Reset();

        // 5 читателей, 3 писателя, каждый делает 100 операций
        var (finalValue, _) = await ServerForTests.RunMixedLoad(5, 3, 100);

        // Писатели добавили 3 * 100 = 300
        Assert.Equal(300, finalValue);
    }

    [Fact]
    public async Task Test_ConcurrentReads_DoNotBlockEachOther()
    {
        ServerForTests.Reset();
        ServerForTests.DisableTestMode();

        // Замеряем время выполнения 100 читателей
        var sw = System.Diagnostics.Stopwatch.StartNew();
        await ServerForTests.RunMultipleReaders(100, 10);
        sw.Stop();

        // Если бы читатели блокировали друг друга, время было бы ~100 * 10 * 1ms = 1000ms
        // При параллельном чтении должно быть значительно меньше
        Assert.True(sw.ElapsedMilliseconds < 500, $"Время выполнения: {sw.ElapsedMilliseconds}ms");
    }
}