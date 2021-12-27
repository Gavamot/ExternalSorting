using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Domain;
using ExternalSortingGen;
using NUnit.Framework;

namespace ExternalSorting.Test;

public class SortingTest
{
    private long Measure(Action act)
    {
        var t = Stopwatch.StartNew();
        act();
        return t.ElapsedMilliseconds;
    }

    private List<StringLine[]> GenerateChunkArray(int chunkSize, int chunkCount)
    {
        var dataArr = new List<StringLine[]>();
        for (int i = 0; i < chunkCount; i++)
        {
            dataArr.Add(Generator.GenerateLinesArray(chunkSize)); 
        }
        return dataArr;
    }
    
    
    [Test]
    public void OptimalChunkSize()
    {
        long Test(List<StringLine[]> arr) => Measure(() => arr.ForEach(Sorting.HeapSort));

        long Check(int chunkSize, int chunkCount)
        {
            var res = Test(GenerateChunkArray(chunkSize, chunkCount));
            Console.WriteLine($"Time part_size[{chunkSize}] - {res} ms");
            return res;
        }

        var t1 = Check(800, 10);
        var t2 = Check(80, 100);
        var t3 = Check(10, 800);
        
        Assert.IsTrue(t1 > t2);
        Assert.IsTrue(t2 > t3);
    }

    private long TestSorting(string name, Action<StringLine[]> sort)
    {
        var data = Generator.GenerateLines(500_000, 123).ToArray(); 
        Console.WriteLine($"START {name}");
        var t = Stopwatch.StartNew();
        sort(data);
        var res = t.ElapsedMilliseconds;
        Console.WriteLine($"END {name} - {res}");
        return res;
    }
    
    /*[Test]
    public void TestSorting()
    {
        var res2 = TestSorting("Merge", Sorting.MergeSort);
        var res3 = TestSorting("Tim", Sorting.TimSort);
        var res4 = TestSorting("QuickSort", Sorting.QuickSort.Sort);
        
        var res1 = TestSorting("Heap", Sorting.HeapSort);
       
        Assert.Less(res1, res2);
        Assert.Less(res1, res3);
        Assert.Less(res4, res1);
    }*/
    
}