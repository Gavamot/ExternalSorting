using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Domain;
using ExternalSortingGen;
using NUnit.Framework;

namespace ExternalSorting.Test;


public class ChunkProducerTest
{
    private string MergeToFile(string[] paths)
    {
        StringBuilder res = new(); 
        foreach (var path in paths)
        {
            res.Append(File.ReadAllText(path));
        }
        return res.ToString();
    }

    [Test]
    public async Task Same20Strings_OneStringInChunk_Created20Chunks()
    {
        var file = "./data/input2.txt";
        ChunkProducer producer = new (file, 256, 21);
        var chunks = await producer.CreateChunks();
        var actual = MergeToFile(chunks);
        var expected = await File.ReadAllTextAsync(file);
        Assert.AreEqual(expected, actual);
    }
    
    
    [Test]
    public async Task DifferentStrings_DifferentChunks_ChunksCreated()
    {
        var file = "./data/input.txt";
        ChunkProducer producer = new (file, 256, 21);
        var chunks = await producer.CreateChunks();
        var actual = MergeToFile(chunks);
        var expected = await File.ReadAllTextAsync(file);
        Assert.AreEqual(expected, actual);
    }
}