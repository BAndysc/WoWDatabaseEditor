using WDE.SqlWorkbench.Models;

namespace WDE.SqlWorkbench.Test.Models;

public class PublicMySqlDecimalTests
{
    [Test]
    public void TestDecimals()
    {
        AssertDecimal(PublicMySqlDecimal.Parse("-1"), true, "1", "");
        AssertDecimal(PublicMySqlDecimal.Parse("1"), false, "1", "");
        AssertDecimal(PublicMySqlDecimal.Parse("0"), false, "0", "");
        AssertDecimal(PublicMySqlDecimal.Parse("0.0"), false, "0", "");
        Assert.Throws<FormatException>(() => PublicMySqlDecimal.Parse("-0.0")); // negative zero is not allowed
        AssertDecimal(PublicMySqlDecimal.Parse("0.1"), false, "0", "1");
        AssertDecimal(PublicMySqlDecimal.Parse("0.01"), false, "0", "01");
        AssertDecimal(PublicMySqlDecimal.Parse("0.001"), false, "0", "001");
        AssertDecimal(PublicMySqlDecimal.Parse("0.0010"), false, "0", "001");
        AssertDecimal(PublicMySqlDecimal.Parse("0.00100"), false, "0", "001");
        AssertDecimal(PublicMySqlDecimal.Parse("-0.00100"), true, "0", "001");
        AssertDecimal(PublicMySqlDecimal.Parse("-000.00100"), true, "0", "001");
        AssertDecimal(PublicMySqlDecimal.Parse("-10"), true, "10", "");
        AssertDecimal(PublicMySqlDecimal.Parse("-10.0"), true, "10", "");
        AssertDecimal(PublicMySqlDecimal.Parse("99999999999999999999999999999999999999999999999999999999999999999"), false, "99999999999999999999999999999999999999999999999999999999999999999", "");
        AssertDecimal(PublicMySqlDecimal.Parse("-99999999999999999999999999999999999999999999999999999999999999999"), true, "99999999999999999999999999999999999999999999999999999999999999999", "");
    }

    [Test]
    public void TestDecimalsComparisons()
    {
        for (double x = -10; x < 10; x += 0.3)
        {
            for (double y = -10; y < 10; y += 0.3)
            {
                var a = PublicMySqlDecimal.FromDecimal((decimal)x);
                var b = PublicMySqlDecimal.FromDecimal((decimal)y);
                Assert.AreEqual(Math.Sign(x.CompareTo(y)), Math.Sign(a.CompareTo(b)), 0, $"x={x}, y={y}");
            }
        }
    }

    [Test]
    public void TestDecimalsComparisons_Random()
    {
        var seed = Random.Shared.Next();
        Console.WriteLine("Testing with seed: " + seed);
        var random = new Random(seed);
        void DrawRandom(out PublicMySqlDecimal dec, out decimal dec2)
        {
            var isNegative = random.Next(0, 2) == 0;
            var whole = random.Next(0, 1000000000);
            var fraction = random.Next(0, 1000000000);
            dec = PublicMySqlDecimal.Parse((isNegative ? "-": "") + whole + "." + fraction);
            dec2 = decimal.Parse((isNegative ? "-": "") + whole + "." + fraction);
        }
        
        for (int i = 0; i < 1000000; ++i)
        {
            DrawRandom(out var a, out var x);
            DrawRandom(out var b, out var y);
            Assert.AreEqual(Math.Sign(x.CompareTo(y)), Math.Sign(a.CompareTo(b)), 0, $"x={x}, y={y}");
        }
    }

    private void AssertDecimal(PublicMySqlDecimal dec, bool isNegative, string whole, string fraction)
    {
        Assert.AreEqual(isNegative, dec.IsNegative);
        Assert.AreEqual(whole, dec.Whole);
        Assert.AreEqual(fraction, dec.Fraction);
    }
}