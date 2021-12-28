using System.Runtime.CompilerServices;

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
     
     [MethodImpl(MethodImplOptions.AggressiveInlining)]
     public int GetLength() => end - start + 1;
     
     public bool IsMore(StringLine obj) => CompareTo(obj) > 0;
     
     // I a'm sorry for that code works only for ASCII and String must have only first letter in UpperCase
     // Dont use IComparable for avoid boxing/unboxing operations
     public int CompareTo(StringLine obj)
     {
          int ln1 = GetLength();
          int ln2 = obj.GetLength();
          // This is Bad to batter save it in the class variable but I want to save memory
          int ds1 = GetSeparator();
          int ds2 = obj.GetSeparator();
          
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
               if (line[index1] > obj.line[index2]) return -1;
               if (line[index1] < obj.line[index2]) return 1;
          }

          if (strLn1  > minSize) return -1;
          if (strLn2 > minSize) return 1;

          // Compare numbers
         
          if (ds1 > ds2) return -1;
          if (ds1 < ds2) return 1;

          int lnNum1 = ln1 - strLn1;
          int lnNum2 = ln2 - strLn2;
          minSize = Math.Min(lnNum1, lnNum2);
          if (lnNum2 > minSize) return 1;
          if (lnNum1 > minSize) return -1;
          for (int i = 0; i < minSize; i++)
          {
               int index1 = start + i;
               int index2 = obj.start + i;
               if (line[index1] > obj.line[index2]) return -1;
               if (line[index1] < obj.line[index2]) return 1;
          }
          return 0;
     }
     
     [MethodImpl(MethodImplOptions.AggressiveInlining)]
     private int GetSeparator()
     {
          for (int i = start + 1; i < Global.MaxDotPosition; i++)
          {
               if (line[i] == AsciiCodes.Dot) return i;
          }
          throw new Exception("Can not find string parts separator. Check the source file for correct");
     }
     
     public static bool operator <=(StringLine a, StringLine b) => a.CompareTo(b) <= 0;
     public static bool operator >=(StringLine a, StringLine b) => a.CompareTo(b) >= 0;
     public static bool operator >(StringLine a, StringLine b) => a.CompareTo(b) > 0;
     public static bool operator <(StringLine a, StringLine b)  => a.CompareTo(b) < 0;
}