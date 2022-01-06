using System.Buffers;

namespace Domain;

public static class BinaryMath
{
    public static long MbsToBytes(this int mb) => mb * 1024L * 1024L;  
    public static long MbsToBytes(this long mb) => mb * 1024L * 1024L;  
    public static int BytesToMbs(this long bytes) => (int)(bytes / 1024 / 1024);   
    public static int BytesToMbs(this int bytes) => bytes / 1024 / 1024;
    public static int ToNearestPow2(int n)
    {
        bool IsPowerOfTwo(int v) => (v & (v - 1)) == 0;
   
        int ToNextNearest(int x)
        {
            if (x < 0) return 0;
            --x;
            x |= x >> 1;
            x |= x >> 2;
            x |= x >> 4;
            x |= x >> 8;
            x |= x >> 16;
            return x + 1;
        }
        int next = ToNextNearest(n);
        int prev = next >> 1;
        return IsPowerOfTwo(n) ? n : prev;
    }
}