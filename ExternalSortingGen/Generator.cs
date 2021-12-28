using System.Text;
using Domain;

namespace ExternalSortingGen;

public static class Generator
{
    public static void WriteToFile(string path, ulong count, int butchSize)
    {
        File.Delete(path);
        ulong done = 0;
        var rnd = new Random(123);
        while (done < count)
        {
            ulong rest = count - done;
            int need = (int) (rest > (ulong) butchSize ? (ulong) butchSize : rest);
            var str = new StringBuilder();
            for (int i = 0; i < need; i++)
            {
                var randomWord = Global.StrArray[rnd.Next(0, Global.StrArray.Length)];
                str.Append($"{rnd.Next(0, 10000)}.{randomWord}\n\r");
            }
            File.AppendAllText(path, str.ToString(), Encoding.ASCII);
            done += (ulong) need;
        }
    }
    
}