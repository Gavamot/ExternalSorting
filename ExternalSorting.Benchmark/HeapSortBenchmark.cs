using System.Text;
using BenchmarkDotNet.Attributes;
using Domain;

namespace ExternalSorting.Benchmark;

[MemoryDiagnoser]
public class HeapSortBenchmark
{
    private const int Size = 500_000;
    private readonly StringLine[] data = new StringLine[Size];

    static readonly string[] strArray = new[]
    {
        "Something something something",
        "Cherry is the best",
        "Banana is yellow",
        "Apple",
        "Blueberry is better than the best. It's a fact. Nobody can argue with that",
        "Strawberry is very delicious",
        "Pineapple is sweet and tasty",
        "What you see is what you get"
    };

    public static string GetRandomString(int length)
    {
        var random = new Random(67434334);
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    static string GenerateDictString()
    {
        var random = new Random(67434334);
        return strArray[random.Next(0, strArray.Length)];
    }

    /*private static StringLine GetRandomStringLine()
    {
        var random = new Random(67434334);
        var num = random.Next();
        var str = GenerateDictString();
        return new StringLine(Encoding.ASCII.GetBytes($"{num}.{str}"));
    }*/

    /*private void GenerateLines()
    {
        for (int i = 0; i < Size; i++)
        {
            data[i] = GetRandomStringLine();
        }
    }*/

    /*public HeapSortBenchmark()
    {
        GenerateLines();
    }
    
    
    [Benchmark]
    public void QuickSort()
    {
        Sorting.QuickSort.Sort(data);
    }
    
    [Benchmark]
    public void HeapSort()
    {
        Sorting.HeapSort(data);
    }
    
    [Benchmark]
    public void MergeSort()
    {
        Sorting.MergeSort(data);
    }
    
    [Benchmark]
    public void TimSort()
    {
        Sorting.TimSort(data);
    }*/
}