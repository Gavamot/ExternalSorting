using System.Buffers;
using System.Diagnostics;
using Domain;

//var maxProduce = Environment.ProcessorCount * 2;
//var chunkSizeBytes = 1024 * 1024 * 100;

const string output = "./output.txt"; 
try
{
    var t = Stopwatch.StartNew();
    var config = new ChunkProducerConfig();
    var chunkProducer = new ChunkProducer(config);
    var chunks = await chunkProducer.CreateChunks();
    
    var chunkMerger = new ChunkMerger();
    await chunkMerger.MergeChunksAsync(output, config.ChunkFolder, chunks);
    
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