using NUnit.Framework;
using WDE.Common.Utils;

namespace WDE.Common.Test.Utils;

public class TokenizerTests
{
    [Test]
    public void TestSimple()
    {
        Tokenizer tokenizer = new("a b c");
        Assert.AreEqual("a", tokenizer.Next().ToString());
        Assert.AreEqual("b", tokenizer.Next().ToString());
        Assert.AreEqual("c", tokenizer.Next().ToString());
        Assert.AreEqual("", tokenizer.Next().ToString());
    }
    
    [Test]
    public void TestSimpleQuotes()
    {
        Tokenizer tokenizer = new("\"a b\" c \"d e\" f");
        Assert.AreEqual("a b", tokenizer.Next().ToString());
        Assert.AreEqual("c", tokenizer.Next().ToString());
        Assert.AreEqual("d e", tokenizer.Next().ToString());
        Assert.AreEqual("f", tokenizer.Next().ToString());
        Assert.AreEqual("", tokenizer.Next().ToString());
    }
    
    [Test]
    public void TestEscape()
    {
        Tokenizer tokenizer = new("\"a\\\\b\"");
        Assert.AreEqual("a\\b", tokenizer.Next().ToString());
    }
    
    [Test]
    public void TestSimpleQuotesEscaped()
    {
        Tokenizer tokenizer = new("\"a \\\" b\" c \"d \\\\ e\" f");
        Assert.AreEqual("a \" b", tokenizer.Next().ToString());
        Assert.AreEqual("c", tokenizer.Next().ToString());
        Assert.AreEqual("d \\ e", tokenizer.Next().ToString());
        Assert.AreEqual("f", tokenizer.Next().ToString());
        Assert.AreEqual("", tokenizer.Next().ToString());
    }

    [Test]
    public void TestRemaining()
    {
        Tokenizer tokenizer = new("/open \"Test\" Ride Spell Name");
        tokenizer.Next();
        tokenizer.Next();
        Assert.AreEqual("Ride Spell Name", tokenizer.Remaining());
    }
}