using System.Runtime.CompilerServices;

namespace Domain;

public record StringLineOptimal
{
    public readonly byte[] line;
    static readonly long NewLineLength = Environment.NewLine.Length;
     
    public readonly long start;
    public readonly long end;
    public long Length => end - start + 1;

    public StringLineOptimal(byte[] line,long start, long end)
    {
        this.line = line;
        this.start = start;
        this.end = end;
    }

    public bool IsMore(StringLineOptimal obj) => CompareTo(obj) > 0;
     
    // I a'm sorry for that code works only for ASCII and String must have only first letter in UpperCase
    // Dont use IComparable for avoid boxing/unboxing operations
    public int CompareTo(StringLineOptimal obj)
    {
        // This is bad Bad code but shod be faster then always public int Length => checked((int)Unsafe.As<RawArrayData>(this).Length);
        long ln1 = Length;
        long ln2 = obj.Length;
        // This is Bad to batter save it in the class variable but I want to save memory
        long ds1 = GetSeparator();
        long ds2 = obj.GetSeparator();
          
        // CompareStrings
        // String must have only first letter in UpperCase
        // -1 \n
          
        long strLn1 = ln1 - ds1 - NewLineLength;
        long strLn2 = ln2 - ds2 - NewLineLength;
        long minSize = Math.Min(strLn1, strLn2);
        for (long i = 1; i < minSize; i++)
        {
            long i1 = ds1 + i + start;
            long i2 = ds2 + i + obj.start;
            if (line[i1] > line[i2]) return -1;
            if (line[i1] < line[i2]) return 1;
        }

        if (strLn1  > minSize) return -1;
        if (strLn2 > minSize) return 1;

        // Compare numbers
         
        if (ds1 > ds2) return -1;
        if (ds1 < ds2) return 1;

        long lnNum1 = ln1 - strLn1;
        long lnNum2 = ln2 - strLn2;
        minSize = Math.Min(lnNum1, lnNum2);
        if (lnNum2 > minSize) return 1;
        if (lnNum1 > minSize) return -1;
        for (long i = 0; i < minSize; i++)
        {
            long i1 = start + i;
            long i2 = obj.start + i;
            if (line[i1] > line[i2]) return -1;
            if (line[i1] < line[i2]) return 1;
        }
        return 0;
    }
     
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private long GetSeparator()
    {
        // Number will be in any case because of that start with index 1 
        const byte dotAsciiNumber = 46; // ASCII "." number is 46
        for (long i = 1; i < Length; i++)
        {
            if (line[start + i] == dotAsciiNumber) return i;
        }
        throw new Exception("Can not find string parts separator. Check the source file for correct");
    }
     
    public static bool operator <=(StringLineOptimal a, StringLineOptimal b) => a.CompareTo(b) <= 0;
    public static bool operator >=(StringLineOptimal a, StringLineOptimal b) => a.CompareTo(b) >= 0;
    public static bool operator >(StringLineOptimal a, StringLineOptimal b) => a.CompareTo(b) > 0;
    public static bool operator <(StringLineOptimal a, StringLineOptimal b)  => a.CompareTo(b) < 0;
}