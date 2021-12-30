using System.Buffers;
using System.Diagnostics;
using Domain;

//var maxProduce = Environment.ProcessorCount * 2;
//var chunkSizeBytes = 1024 * 1024 * 100;

const string output = "./output.txt"; 
try
{
    var t = Stopwatch.StartNew();
    GC.Collect(2, GCCollectionMode.Forced, true, true);
    var chunkProducer = new ChunkProducer("./input.txt", 256, 1);
    var chunks = await chunkProducer.CreateChunks();
    
    var chunkMergerConfig = new ChunkMergerConfig();
    var chunkMerger = new ChunkMerger(chunkMergerConfig);
    await chunkMerger.MergeChunks(output, chunks);
    
    Console.WriteLine($"IT IS DONE HAVE SPEND {t.Elapsed.Minutes} minutes");
}
catch (AppException e)
{
    e.Print();
}
catch (Exception e)
{
    Console.WriteLine(e.Message);
}