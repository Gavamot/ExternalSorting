using System.Text;
using Domain;

namespace ExternalSortingGen;

public static class Generator
{
    public static string GenerateRandomString(this Random rnd)
    {
        var randomWord = Global.StrArray[rnd.Next(0, Global.StrArray.Length)];
        var randomNumber = rnd.Next(Global.MinNumber, Global.MaxNumber);
        return $"{randomNumber}.{randomWord}\r\n";
    }
    public static ChunkProducer.Chunk GenerateRandomInMemoryTest(long bytesCount, int seed)
    {
        var rnd = new Random(seed);
        long cur = 0;
        StringBuilder builder = new ();
        do
        {
            var str = rnd.GenerateRandomString();
            cur += str.Length;
            builder.Append(str);
        } while (cur < bytesCount);

        ChunkProducer.Chunk chunk = new();
        var text = builder.ToString();
        chunk.Line = Encoding.ASCII.GetBytes(text);
        chunk.Start = 0;
        chunk.End = text.Length - 1;
        return chunk;
    }

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
    
    public static void WriteToFile(string output, long count, int butchSize, int seed)
    {
        long done = 0;
        var rnd = new Random(seed);
        while (done < count)
        {
            long rest = count - done;
            int need = (int) (rest > butchSize ? butchSize : rest);
            var str = new StringBuilder();
            for (int i = 0; i < need; i++)
            {
                str.Append(rnd.GenerateRandomString());
            }
            File.AppendAllText(output, str.ToString(), Encoding.ASCII);
            done += need;
        }
    }
    
    public static void WriteToFileMb(string output, long mb, int seed)
    {
        long done = 0;
        var rnd = new Random(seed);
        long bytes = mb * 1024 * 1024;
        int bufSize = 10 * 1024 * 1024;
        while (done < bytes)
        {
            int cur = 0;
            long rest = bytes - done;
            int need = (int) (rest > bufSize ? bufSize : rest);
            var str = new StringBuilder();
            while (cur < need)
            {
                var word = rnd.GenerateRandomString();
                cur += word.Length;
                str.Append(word);
            }
            File.AppendAllText(output, str.ToString(), Encoding.ASCII);
            done += cur;
        }
    }
}