using Xunit;
using System.Threading.Tasks;

public class ServerTests
{
    [Fact]
    public async Task Test_ReadersWorkInParallel()
    {
        ServerForTests.Reset();
        ServerForTests.EnableTestMode(delayMs: 10);

        // «апускаем 10 читателей и провер€ем, сколько работают одновременно
        int maxParallelReaders = await ServerForTests.TestReadersParallelism(10, 200);

        // ƒолжно быть больше 1 (значит работали параллельно)
        Assert.True(maxParallelReaders > 1, $"ћаксимум параллельных читателей: {maxParallelReaders}");
    }

    [Fact]
    public async Task Test_WritersWorkSequentially()
    {
        ServerForTests.Reset();

        // «апускаем 5 писателей
        int maxParallelWriters = await ServerForTests.TestWritersSequential(5, 200);

        // ƒолжно быть не больше 1
        Assert.True(maxParallelWriters <= 1, $"ћаксимум параллельных писателей: {maxParallelWriters}");
    }

    [Fact]
    public async Task Test_ReadersWaitForWriters()
    {
        bool readersWaited = await ServerForTests.TestReadersWaitForWriters();
        Assert.True(readersWaited, "„итатели должны ждать окончани€ записи");
    }

    [Fact]
    public async Task Test_MixedLoad_Correctness()
    {
        ServerForTests.Reset();

        // 5 читателей, 3 писател€, каждый делает 100 операций
        var (finalValue, _) = await ServerForTests.RunMixedLoad(5, 3, 100);

        // ѕисатели добавили 3 * 100 = 300
        Assert.Equal(300, finalValue);
    }

    [Fact]
    public async Task Test_ConcurrentReads_DoNotBlockEachOther()
    {
        ServerForTests.Reset();
        ServerForTests.DisableTestMode();

        // «амер€ем врем€ выполнени€ 100 читателей
        var sw = System.Diagnostics.Stopwatch.StartNew();
        await ServerForTests.RunMultipleReaders(100, 10);
        sw.Stop();

        // ≈сли бы читатели блокировали друг друга, врем€ было бы ~100 * 10 * 1ms = 1000ms
        // ѕри параллельном чтении должно быть значительно меньше
        Assert.True(sw.ElapsedMilliseconds < 500, $"¬рем€ выполнени€: {sw.ElapsedMilliseconds}ms");
    }
}