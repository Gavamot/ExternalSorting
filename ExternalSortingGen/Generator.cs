using System.Text;
using Domain;

namespace ExternalSortingGen;

public static class Generator
{
    public static void WriteTestToFile(string output, string test, int proposalCount, int seed)
    {
        if (proposalCount > Global.StrArray.Length) throw new AppException($"Have not enough proposals max is {Global.StrArray.Length}");
        var outStrings = new List<string>(proposalCount * Global.MaxNumber);
        var words = Global.StrArray.OrderBy(x=>x).ToArray();
        
        for (int i = 0; i < proposalCount; i++)
        {
            var str = new StringBuilder();
            for (int j = 1; j < Global.MaxNumber; j++)
            {
                var randomWord = words[i];
                var _ = $"{j}.{randomWord}\r\n";
                str.Append(_);
                outStrings.Add(_);
            }
            File.AppendAllText(test, str.ToString(), Encoding.ASCII);
        }
        WriteUnsortedForTest(output, outStrings, seed);
    }

    private static void WriteUnsortedForTest(string test, List<string> outStrings, int seed)
    {
        StringBuilder unsorted = new ();
        var rnd = new Random(seed);
        
        while (outStrings.Count > 0)
        {
            int index = rnd.Next(0, outStrings.Count);
            var str = outStrings[index];
            unsorted.Append(str);
            outStrings.RemoveAt(index);
        }
        
        File.AppendAllText(test, unsorted.ToString(), Encoding.ASCII);
    }
    
    public static void WriteToFile(string output, ulong count, int butchSize, int seed)
    {
        ulong done = 0;
        var rnd = new Random(seed);
        while (done < count)
        {
            ulong rest = count - done;
            int need = (int) (rest > (ulong) butchSize ? (ulong) butchSize : rest);
            var str = new StringBuilder();
            for (int i = 0; i < need; i++)
            {
                var randomWord = Global.StrArray[rnd.Next(0, Global.StrArray.Length)];
                var randomNumber = rnd.Next(Global.MinNumber, Global.MaxNumber);
                str.Append($"{randomNumber}.{randomWord}\r\n");
            }
            File.AppendAllText(output, str.ToString(), Encoding.ASCII);
            done += (ulong) need;
        }
    }
    
}