using System.Diagnostics;

namespace ExternalSorting
{
    internal static class MemoryInfo
    {
       
        
        static MemoryInfo()
        {
            Reset();
        }

        public static long MaximumMemoryAmount { get; private set; }

        public static void Reset()
        {
            Process proc = Process.GetCurrentProcess();
            MaximumMemoryAmount = proc.PrivateMemorySize64;
        }

        public static float GetOccupiedMemoryPercent()
        {
            return (float) GC.GetTotalMemory(false)/MaximumMemoryAmount;
        }

        public static long GetFreeMemoryLeft()
        {
            return MaximumMemoryAmount - GC.GetTotalMemory(false);
        }
    }
}