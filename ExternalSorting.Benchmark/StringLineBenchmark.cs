using System;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Attributes;

namespace ExternalSorting.Test;

[MemoryDiagnoser]
public class StringLineBenchmark
{
    private const int Size = 1000000;
    private readonly StringLine[] data = new StringLine[Size];

    private static Random random = new Random(67434334);
    public static string GetRandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    private static StringLine GetRandomStringLine()
    {
        var num = random.Next();
        var str = GetRandomString(random.Next(10, 30)).ToLower();
        return new StringLine(Encoding.ASCII.GetBytes($"{num}.{str}"));
    }
    
    public StringLineBenchmark()
    {
        var v = new Random(4535453);
        for (int i = 0; i < Size; i++)
        {
            data[i] = GetRandomStringLine();
        }
    }
    
    [Benchmark]
    public void TestBenchMark()
    {
        for (int i = 1; i < Size; i++)
        {
            data[i - 1].CompareTo(data[i]);
        }
    }
}