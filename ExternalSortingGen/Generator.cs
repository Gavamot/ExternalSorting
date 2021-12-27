using System.Text;

namespace ExternalSortingGen;

public static class Generator
{
    static readonly string[] strArray = {
        "Something something something",
        "Cherry is the best",
        "Banana is yellow",
        "Apple",
        "Blueberry is better than the best",
        "Strawberry is very delicious",
        "Pineapple is sweet and tasty",
        "What you see is what you get"
    };
    
    public static void WriteToFile(string path, ulong count, int butchSize)
    {
        ulong done = 0;
        var rnd = new Random(123);
        while (done < count)
        {
            ulong rest = count - done;
            int need = (int) (rest > (ulong) butchSize ? (ulong) butchSize : rest);

            var str = new StringBuilder();
            for (int i = 0; i < need; i++)
            {
                str.Append($"{rnd.Next(0, 10000)}.{strArray[rnd.Next(0, strArray.Length)]}");
            }
            File.AppendAllText(path, str.ToString(), Encoding.ASCII);
            done += (ulong) need;
        }
    }
}