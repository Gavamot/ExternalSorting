using System.Runtime.CompilerServices;

namespace ExternalSorting;

public record StringLine
{
     public StringLine(byte[] line)
     {
          Line = line;
     }
     
     public readonly byte[] Line;

     // I a'm sorry for that code works only for ASCII and String must have only first letter in UpperCase
     // Dont use IComparable for avoid boxing/unboxing operations
     public int CompareTo(StringLine obj)
     {
          // This is bad Bad code but shod be faster then always public int Length => checked((int)Unsafe.As<RawArrayData>(this).Length);
          int ln1 = Line.Length;
          int ln2 = obj.Line.Length;
          // This is Bad to batter save it in the class variable but I want to save memory
          int ds1 = GetSeparator();
          int ds2 = obj.GetSeparator();
          
          // CompareStrings
          // String must have only first letter in UpperCase
          int strLn1 = ln1 - ds1;
          int strLn2 = ln2 - ds2;
          int minSize = Math.Min(strLn1, strLn2);
          for (int i = 1; i < minSize; i++)
          {
               if (Line[ds1 + i] > obj.Line[ds2 + i]) return -1;
               if (Line[ds1 + i] < obj.Line[ds2 + i]) return 1;
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
               if (Line[i] > obj.Line[i]) return -1;
               if (Line[i] < obj.Line[i]) return 1;
          }
          return 0;
     }
     
     [MethodImpl(MethodImplOptions.AggressiveInlining)]
     private int GetSeparator()
     {
          // Number will be in any case because of that start with index 1 
          const byte dotAsciiNumber = 46; // ASCII "." number is 46
          for (int i = 1; i < Line.Length; i++)
          {
               if (Line[i] == dotAsciiNumber) return i;
          }
          throw new Exception("Can not find string parts separator. Check the source file for correct");
     }
}