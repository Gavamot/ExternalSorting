using System;
using System.Linq;
using System.Text;
using Domain;
using NUnit.Framework;

namespace ExternalSorting.Test;

public class StringLineOptimalTest
{
    private static (StringLineOptimal str1, StringLineOptimal str2) CreateStr(string str, string str2)
    {
        var arr1 = Encoding.ASCII.GetBytes(str + Environment.NewLine);
        var arr2 = Encoding.ASCII.GetBytes(str2 + Environment.NewLine);
        var line = arr1.Concat(arr2).ToArray();
        return (new StringLineOptimal(line,0, arr1.LongLength - 1), new StringLineOptimal(line, arr1.Length, arr1.Length + arr2.Length - 1));
    }

    private void TestCompare(string line1, string line2, int expected)
    {
        var (str1, str2) = CreateStr(line1, line2);
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