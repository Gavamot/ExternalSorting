using System.Text;

namespace Domain;

public class AsciiCodes
{
    public const byte LineEnd = 10;
    public const byte Dot = 46;
}

public static class Global
{
    public static readonly string[] StrArray = {
        "Something something something",
        "Cherry is the best",
        "Banana is yellow",
        "Apple",
        "Blueberry is better than the best",
        "Strawberry is very delicious",
        "Pineapple is sweet and tasty",
        "What you see is what you get"
    };
    static readonly string LargestString = StrArray.MaxBy(y=> y.Length) ?? throw new Exception("StrArray is empty");
    public static readonly int MinStringLength = "1.A\n\r".Length;
    public static readonly int MaxStringLength = $"{int.MaxValue}.{LargestString}\n\r".Length;
    public static readonly int MidStringLength = (MaxStringLength + MinStringLength) / 2;
    public static readonly int MaxDotPosition = $"{int.MaxValue}".Length + 1;
}