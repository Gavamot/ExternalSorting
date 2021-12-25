using System.Text;
using Domain;

namespace ExternalSortingGen;

public static class Generator
{
    static readonly string[] strArray = {
        "Something something something",
        "Cherry is the best",
        "Banana is yellow",
        "Apple",
        "Blueberry is better than the best. It's a fact. Nobody can argue with that",
        "Strawberry is very delicious",
        "Pineapple is sweet and tasty",
        "What you see is what you get"
    };
    
    private static StringLine GetRandomStringLine(Random rnd)
    {
        var num = rnd.Next(0, 10000);
        var str = strArray[rnd.Next(0, strArray.Length)];
        return new StringLine(Encoding.ASCII.GetBytes($"{num}.{str}{Environment.NewLine}"));
    }

    public static IEnumerable<StringLine> GenerateLines(ulong size, int seed = 123)
    {
        var rnd = new Random(seed);
        for (ulong i = 0; i < size; i++)
        {
           yield return GetRandomStringLine(rnd);
        }
    }

    public static StringLine[] GenerateLinesArray(int size, int seed = 123) => GenerateLines((ulong)size, seed).ToArray();
    
    public static StringLine[] ReadFromFile(string filePath)
        => File.ReadAllLines(filePath).Select(x=> new StringLine(Encoding.ASCII.GetBytes(x))).ToArray();
}