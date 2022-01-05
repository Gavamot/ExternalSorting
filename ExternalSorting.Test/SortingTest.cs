using System;
using System.Collections.Generic;
using System.Diagnostics;
using Domain;
using ExternalSortingGen;
using NUnit.Framework;

namespace ExternalSorting.Test;

public class SortingTest
{
    private long ExecutionTimeMs(long chunkSizeBytes)
    {
        var t = Generator.GenerateRandomInMemoryTest(chunkSizeBytes, 10);
        var sw1 = Stopwatch.StartNew();
        var res = t.GetSortedLines();
        sw1.Stop();
        return sw1.ElapsedMilliseconds;
    }
    
    [Test]
    public void OptimalChunkSize()
    {
        var t1 = ExecutionTimeMs(1024 * 1024 * 5);
        var t2 = ExecutionTimeMs(1024 * 1024 * 5);
        var t3 = ExecutionTimeMs(1024 * 1024 * 10);
        Console.WriteLine($"t1={t1} ms, t2={t2} ms, t3={t3} ms");
        Assert.Less(t1 + t2, t3);
    }
    
}