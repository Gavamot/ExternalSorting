using System.Runtime.CompilerServices;
using System.Text;

namespace Domain;

public record StringLine
{
     public StringLine(byte[] line, int start, int end)
     {
          this.line = line;
          this.start = start;
          this.end = end;
     }
     
     public readonly byte[] line;
     public readonly int start;
     public readonly int end;
     public string View => Encoding.ASCII.GetString(new ReadOnlySpan<byte>(line, start, end - start)); 
     
     [MethodImpl(MethodImplOptions.AggressiveInlining)]
     public int GetLength() => end - start + 1;
     
     public bool IsMore(StringLine obj) => CompareTo(obj) > 0;


     public const int Less = 1;
     public const int More = -1;
     
     // I a'm sorry for that code works only for ASCII and String must have only first letter in UpperCase
     // Dont use IComparable for avoid boxing/unboxing operations
     public int CompareTo(StringLine obj)
     {
          int ln1 = GetLength();
          int ln2 = obj.GetLength();
          // This is Bad to batter save it in the class variable but I want to save memory
          int ds1 = GetSeparatorPosition();
          int ds2 = obj.GetSeparatorPosition();
          
          // CompareStrings
          // String must have only first letter in UpperCase
          // -1 \n
          
          int strLn1 = ln1 - ds1;
          int strLn2 = ln2 - ds2;
          int minSize = Math.Min(strLn1, strLn2);
          
          for (int i = 1; i < minSize; i++)
          {
               int index1 = start + ds1 + i;
               int index2 = obj.start + ds2 + i;
               if (line[index1] > obj.line[index2]) return Less;
               if (line[index1] < obj.line[index2]) return More;
          }

          if (strLn1  > minSize) return Less;
          if (strLn2 > minSize) return More;

          // Compare numbers

          if (ds1 > ds2) return Less;
          if (ds1 < ds2) return More;

          int lnNum1 = ln1 - strLn1;
          int lnNum2 = ln2 - strLn2;
          minSize = Math.Min(lnNum1, lnNum2);
          if (lnNum2 > minSize) return More;
          if (lnNum1 > minSize) return Less;
          for (int i = 0; i < minSize; i++)
          {
               int index1 = start + i;
               int index2 = obj.start + i;
               if (line[index1] > obj.line[index2]) return Less;
               if (line[index1] < obj.line[index2]) return More;
          }
          return 0;
     }
     
     [MethodImpl(MethodImplOptions.AggressiveInlining)]
     private int GetSeparatorPosition()
     {
          int maxEndLine = start + Global.MaxDotPosition;
          for (int i = start + 1; i <= maxEndLine; i++)
          {
               if (line[i] == AsciiCodes.Dot) return i - start;
          }
          throw new Exception("Can not find string parts separator. Check the source file for correct");
     }
     
     public static bool operator <=(StringLine a, StringLine b) => a.CompareTo(b) <= 0;
     public static bool operator >=(StringLine a, StringLine b) => a.CompareTo(b) >= 0;
     public static bool operator >(StringLine a, StringLine b) => a.CompareTo(b) > 0;
     public static bool operator <(StringLine a, StringLine b)  => a.CompareTo(b) < 0;
}