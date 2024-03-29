﻿using System;
using System.IO;
using System.Threading.Tasks;
using Domain;
using NUnit.Framework;

namespace ExternalSorting.Test;

public class ChunkMerger
{
    const string Output = "./data/output.txt";
    private const string ChunksFolder = "./data/chunks";
    [TearDown]
    public void TearDown()
    {
        File.Delete(Output);
    }
    
    [Test]
    public async Task CheckExternalSorting_HugeFile()
    {
        ChunkProducer producer = new(new ChunkProducerConfig()
        {
            Input = "C:\\dev\\ExternalSorting\\ExternalSortingGen\\bin\\Release\\net6.0\\publish\\input.txt",
            ChunkFolder = "./data/chunks",
            ChunkSize = 1024 * 1024 * 128,
            MaxProduce = Environment.ProcessorCount
        });
        
        GC.Collect(2, GCCollectionMode.Forced, true, true);
        var chunks = await producer.CreateChunks();
        producer = null;
        
        GC.Collect(2, GCCollectionMode.Forced, true, true);
        var merger = new Domain.ChunkMerger();
        await merger.MergeChunksAsync(Output, ChunksFolder, chunks);
    }
    
    [TestCase("./data/input.txt", "./data/test.txt", 1024 * 1024, 16)]
    [TestCase("./data/input2.txt", "./data/test2.txt", 1024 * 1024, 16)]
    [TestCase("./data/input.txt", "./data/test.txt", 1024 * 1024 * 10, 1)]
    [TestCase("./data/input.txt", "./data/test.txt", 1024 * 1024 * 10, 44)]
    public async Task CheckExternalSorting_RandomValues(string input, string test, int chunkSize, int maxProduce)
    {
        ChunkProducer producer = new(new ChunkProducerConfig()
        {
            Input = input,
            ChunkFolder = "./data/chunks",
            ChunkSize = chunkSize,
            MaxProduce = maxProduce
        });
        
        GC.Collect(2, GCCollectionMode.Forced, true, true);
        var chunks = await producer.CreateChunks();
        
        GC.Collect(2, GCCollectionMode.Forced, true, true);
        var merger = new Domain.ChunkMerger();
        await merger.MergeChunksAsync(Output, ChunksFolder, chunks);
        
        var actual = await File.ReadAllTextAsync(Output);
        var expected = await File.ReadAllTextAsync(test);
        Assert.AreEqual(expected, actual);
        
    }

}