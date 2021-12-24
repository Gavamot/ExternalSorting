using System.Text;
using NUnit.Framework;

namespace ExternalSorting.Test;

public class StringLineTest
{
    private static StringLine CreateStr(string str) => new StringLine(Encoding.ASCII.GetBytes(str));

    private void TestCompare(string line1, string line2, int expected)
    {
        var str1 = CreateStr(line1);
        var str2 = CreateStr(line2);
        var actual = str1.CompareTo(str2);
        Assert.AreEqual(expected, actual);
    }

    [TestCase("1234.Apple", "123.Apple", -1)]
    [TestCase("123.Apple", "1234.Apple", 1)]
    [TestCase("123.Apple", "123.Apple", 0)]
    [TestCase("4.Apple", "5.Apple", 1)]
    [TestCase("4.Apple", "3.Apple", -1)]
    public void TestNumbers(string line1, string line2, int expected) => TestCompare(line1, line2, expected);
    
    [TestCase("1.Apple", "1.Bpple", 1)]
    [TestCase("1.Bpple", "1.Apple", -1)]
    [TestCase("1.Apple5678", "1.Apple", -1)]
    [TestCase("1.Apple apple", "1.Apple j", 1)]
    [TestCase("1.Apple", "1.Apple", 0)]
    [TestCase("1.A", "1.Ap", 1)]
    [TestCase("1.Ap", "1.A", -1)]
    public void TestLetters(string line1, string line2, int expected) => TestCompare(line1, line2, expected);
    
}