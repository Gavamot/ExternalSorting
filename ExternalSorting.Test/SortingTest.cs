using System;
using System.Diagnostics;
using Domain;
using ExternalSortingGen;
using NUnit.Framework;

namespace ExternalSorting.Test;

public class SortingTest
{
  
    [Test]
    public void OptimalChunkSize()
    {
        long ExecutionTimeMs(long chunkSizeBytes)
        {
            var t = Generator.GenerateRandomInMemoryTest(chunkSizeBytes, 10);
            var sw1 = Stopwatch.StartNew();
            var res = t.GetSortedLines();
            sw1.Stop();
            return sw1.ElapsedMilliseconds;
        }

        var t1 = ExecutionTimeMs(1024 * 1024 * 5);
        var t2 = ExecutionTimeMs(1024 * 1024 * 5);
        var t3 = ExecutionTimeMs(1024 * 1024 * 10);
        Console.WriteLine($"t1={t1} ms, t2={t2} ms, t3={t3} ms");
        Assert.Less(t1 + t2, t3);
    }
    
    [Test]
    public void CompareSorting_HipSOrtWinForBigData()
    {
        long ExecutionTimeMs(long chunkSizeBytes, Action<StringLine[]> sort)
        {
            var t = Generator.GenerateRandomInMemoryTest(chunkSizeBytes, 10);
            var lines = t.GetSortedLines();
            var sw1 = Stopwatch.StartNew();
            sort(lines);
            sw1.Stop();
            Console.WriteLine(sw1.ElapsedMilliseconds);
            return sw1.ElapsedMilliseconds;
        }

        var h = (long thousands) => ExecutionTimeMs(thousands * 1000 , Sorting.HeapSort);
        var q = (long thousands) => ExecutionTimeMs(thousands * 1000, Sorting.QuickSort.Sort);
        
        var h1 = h(500);
        var q1 = q(500);
        
        Assert.Less(h1, q1);
    }
    
    
    
}