using System.Text;

namespace Domain;

public static class AppFile
{
    public static void TryRemoveFolder(string path)
    {
        try
        {
            if(Directory.Exists(path)) Directory.Delete(path, true);
        }
        catch
        {
            Console.WriteLine("Can not remove chunk folder");
        }
    }
    public static void MustRemove(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                Console.WriteLine($"file ({path}) is already exist. It will be removed");
                File.Delete(path);
            }
        }
        catch (IOException e)
        {
            throw new AppException($"file {path} already exist. Cam not remove it", e);
        }
    }
    public static void TryRemove(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch(Exception e)
        {
            Console.WriteLine($"Can not remove file {path}");
        }
    }
}

public static class Ext
{
    public static void Foreach<T>(this IEnumerable<T> self, Action<T> act)
    {
        foreach (var item in self)
        {
            act(item);
        }
    }
}

public class AsciiCodes
{
    public const byte LineEnd = 10;
    public const byte Dot = 46;
}

public static class Global
{
    public static readonly string[] StrArray = {
        "Something something something. Botrer is very big stadium. You mst tell me it.",
        "Cherry is the best. Cherry is the best. Cherry is the best. NULL is object.",
        "Banana is yellow. I see you faster. Horidor jort verty lor",
        "Apple as banana is very testy fruit. Cilcure jort.",
        "Blueberry is better than the best. Ms by test",
        "Strawberry is very delicious. Send me back it gotra",
        "Pineapple is sweet and tasty. Hahafa is senderey. Segment of memory fully rotated.",
        "What you see is what you get. Jorte fort an my hort. Secret is mine. Semgonro norto est."
    };

    public const int MinNumber = 1; 
    public const int MaxNumber = 10_000; 
    static readonly string LargestString = StrArray.MaxBy(y=> y.Length) ?? throw new Exception("StrArray is empty");
    public static readonly int MinStringLength = "1.A\n\r".Length;
    public static readonly int MaxStringLength = $"{int.MaxValue}.{LargestString}\n\r".Length;
    public static readonly int MidStringLength = (MaxStringLength + MinStringLength) / 2;
    public static readonly int MaxDotPosition = $"{int.MaxValue}".Length + 1;
}