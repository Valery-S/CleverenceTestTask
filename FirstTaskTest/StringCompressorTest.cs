using Xunit;

public class StringCompressorTests
{
    // ========== Тесты для  COMPRESS ==========

    [Fact]
    public void Compress_ValidInput_ReturnsCorrectResult()
    {
        // Arrange
        var input = "aaabbcccdde";

        // Act
        var result = StringCompressor.Compress(input);

        // Assert
        Assert.Equal("a3b2c3d2e", result);
    }

    [Theory]
    [InlineData("a", "a")]                      // один символ
    [InlineData("abc", "abc")]                  // все разные
    [InlineData("aaabbb", "a3b3")]              // две группы
    [InlineData("", "")]                        // пуста¤ строка
    [InlineData("aab", "a2b")]                  // группа в начале
    [InlineData("abb", "ab2")]                  // группа в конце
    [InlineData("aabb", "a2b2")]                // чередование
    [InlineData("aaaaaaaaaa", "a10")]           // 10 одинаковых (двузначное число)
    [InlineData("aaaaaaaaaaa", "a11")]          // 11 одинаковых
    [InlineData("zzzzzzzzzzzzzzzzzzzz", "z20")] // 20 одинаковых
    [InlineData("aaabbbcccccd", "a3b3c5d")]     // разные длины групп
    [InlineData("xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx", "x54")] // 54 символа
    public void Compress_VariousInputs_ReturnsExpected(string input, string expected)
    {
        Assert.Equal(expected, StringCompressor.Compress(input));
    }

    // ========== Тесты для DECOMPRESS ==========

    [Fact]
    public void Decompress_ValidCompressed_ReturnsOriginal()
    {
        // Arrange
        var compressed = "a3b2c3d2e";

        // Act
        var result = StringCompressor.Decompress(compressed);

        // Assert
        Assert.Equal("aaabbcccdde", result);
    }

    [Theory]
    [InlineData("a", "a")]                      // без числа
    [InlineData("abc", "abc")]                  // несколько одиночных
    [InlineData("a3b3", "aaabbb")]              // только группы
    [InlineData("a2b", "aab")]                  // группа в начале
    [InlineData("ab2", "abb")]                  // группа в конце
    [InlineData("a2b2", "aabb")]                // чередование
    [InlineData("a10", "aaaaaaaaaa")]           // двузначное число
    [InlineData("a11", "aaaaaaaaaaa")]          // 11 символов
    [InlineData("z20", "zzzzzzzzzzzzzzzzzzzz")] // 20 символов
    [InlineData("a3b2c5d", "aaabbcccccd")]      // группы разной длины
    [InlineData("", "")]                        // пуста¤ строка
    [InlineData("x1", "x")]                     // число 1 (по условию не должно встречатьс¤, но декомпресси¤ должна работать)
    public void Decompress_VariousInputs_ReturnsExpected(string compressed, string expected)
    {
        Assert.Equal(expected, StringCompressor.Decompress(compressed));
    }

    // ========== Сквозные тесты (Round-trip) ==========
    // ѕровер¤ем, что сжатие + декомпресси¤ дают исходную строку

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("abc")]
    [InlineData("aaabbb")]
    [InlineData("aabbccddeeffgg")]
    [InlineData("aaaaaaaaaaaaaaaaaaaa")]        // 20 a
    [InlineData("abacabadabacaba")]             // палиндром с чередованием
    [InlineData("mmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmmm")] // длинна строка
    public void Roundtrip_CompressThenDecompress_ReturnsOriginal(string original)
    {
        // Act
        string compressed = StringCompressor.Compress(original);
        string decompressed = StringCompressor.Decompress(compressed);

        // Assert
        Assert.Equal(original, decompressed);
    }

    // ========== Дополнительные граничные тесты ==========

    [Fact]
    public void Compress_DoesNotChangeStringWithoutRepeats()
    {
        string input = "abcdefghijklmnopqrstuvwxyz";
        string result = StringCompressor.Compress(input);
        Assert.Equal(input, result);
    }

    [Fact]
    public void Decompress_WithMultidigitNumbers_WorksCorrectly()
    {
        // Искусственно создаём сжатую строку с числами > 9
        string compressed = "a12b5c123";
        string expected = new string('a', 12) + new string('b', 5) + new string('c', 123);
        Assert.Equal(expected, StringCompressor.Decompress(compressed));
    }
}