using System.Buffers;
using System.Diagnostics;

namespace Domain;

public class Config
{
    public int ParallelChunks { get; set; }
    public int ChunkSize { get; set; }
    public string WorkFolder { get; set; }
}

public class ChunkProducer
{
    public ChunkProducer(string sourcePath, int chunkSize, int maxProduce)
    {
        this.sourcePath = sourcePath;
        this.chunkSize = chunkSize;
        this.chunkWriter = new ChunkWriter(maxProduce);
    }

    private readonly int chunkSize; 
    private readonly string sourcePath;
    private readonly ChunkWriter chunkWriter;
    const int AsciiStringLineEnd = 10;

    private int number = 1;
    private const string ChunksFolder = "./chunks";
    string ChunkName => $"{ChunksFolder}/chunk_{number++}.txt";
    public async Task<IEnumerable<Chunk>> CreateChunks()
    {
        try
        {
            if(Directory.Exists(ChunksFolder)) Directory.Delete(ChunksFolder, true);
            Directory.CreateDirectory(ChunksFolder);
        }
        catch (Exception e)
        {
            await Console.Error.WriteLineAsync($"Problems connected with work folder creation {ChunksFolder} There may be problems with the operation of the program. Try to reboot your pc and run this program with administrator privileges.");
            return new Chunk[0];
        }

        List<Chunk> res = new();
        
        await using FileStream fr = new(sourcePath, FileMode.Open, FileAccess.Read);
        
        const int maxStringLength = 128;
        int bufSize = chunkSize - maxStringLength;
        
        var buf1 = ArrayPool<byte>.Shared.Rent(chunkSize); // https://adamsitnik.com/Array-Pool/
        int cur = 0;
        int buf1Red = await fr.ReadAsync(buf1 , 0, bufSize);
        
        Chunk chunk = new()
        {
            Line = buf1,
            Start = 0,
            Path = ChunkName
        };
        res.Add(chunk);
        
        int startNextChunk = 0;
        
        while (buf1Red > 0)
        {
            await chunkWriter.WaitProduce();
            
            var buf2 = ArrayPool<byte>.Shared.Rent(chunkSize);
            var buf2Red = await fr.ReadAsync(buf2, 0, bufSize);
            
            startNextChunk = 0;
            if (buf1[buf1Red - 1] != AsciiStringLineEnd)
            {
                // Complete our Chunk copy packet end to start
                while (startNextChunk < buf2Red)
                {
                    if (buf2[startNextChunk++] == AsciiStringLineEnd)
                    {
                        Array.Copy(buf2, 0, buf1, bufSize, startNextChunk);
                        break;
                    }
                }
            }

            chunk.End = buf1Red + startNextChunk - 1;
            // Execute task on separate thread

            chunkWriter.SortChunkAndWriteToDisk(chunk);
            
            chunk = new Chunk()
            {
                Start = startNextChunk,
                Line = buf2,
                Path = ChunkName
            };
            res.Add(chunk);
            
            buf1 = buf2;
            buf1Red = buf2Red;
        }
        
        chunk.End = buf1Red + startNextChunk - 1;

        if (chunk.Length > 2) chunkWriter.SortChunkAndWriteToDisk(chunk);
        else res.Remove(chunk);
        
        await chunkWriter.WaitWhenAllTaskCompleted();
        return res;
    }
    
    private class ChunkWriter
    {
        public ChunkWriter(int maxProduce)
        {
            this.maxProduce = maxProduce;
        }
        
        private readonly int maxProduce;
        private volatile int produce;
        
        private List<Task> Tasks = new ();
        public void SortChunkAndWriteToDisk(Chunk chunk)
        {
            Interlocked.Increment(ref produce);
            var t = Task.Factory.StartNew(async () =>
            {
                var sw = Stopwatch.StartNew();
                chunk.SortLine();
                await chunk.WriteToDiskAsync();
                chunk.Dispose();
                sw.Stop();
                Console.WriteLine($"{chunk.Path} had been created - {sw.ElapsedMilliseconds} ms");
                await Task.Delay(1000);
                
                Interlocked.Decrement(ref produce);
            }, TaskCreationOptions.LongRunning);
            Tasks.Add(t);
        }
        
        public async Task WaitProduce()
        {
            while (produce >= maxProduce)
            {
                await Task.Delay(1);
            }
        }
        public Task WaitWhenAllTaskCompleted() => Task.WhenAll(Tasks);
    }
}

public class Chunk : IDisposable
{
    public byte[] Line { get; set; }
    public int Start { get; set; }
    public int End { get; set; }
    public int Length => End - Start + 1;
    public string Path { get; set; }
    
    public void SortLine()
    {
        
    }

    public async Task WriteToDiskAsync()
    {
        await using var stream = File.OpenWrite(Path);
        await stream.WriteAsync(Line, Start, Length);
        await stream.FlushAsync();
    }
    
    public void Dispose()
    {
        ArrayPool<byte>.Shared.Return(Line, false);
    }
}

public class StringLineReader
{
    public static Chunk[] Chunks;

    private static int Get2GbOrLess(long memory)
    {
        int gb2 = 209715200;
        return memory >= gb2 ? gb2 : (int)memory;
    }

    public static void AllocateBufferForWrite()
    {
        /*int writeBufSize = Get2GbOrLess((long)(freeMemory * 0.05d));
     ChunkLineWriter.writeBuffer = GC.AllocateUninitializedArray<byte>(writeBufSize, true);
     ChunkLineWriter.writeToFileBuffer = GC.AllocateUninitializedArray<byte>(writeBufSize, true);
     */
    }
    
    public static Chunk[] CreateChunks(string file)
    {
        Console.WriteLine($"Total memory allowed {PcMemory.FreeMemoryMb} mb");
        Console.WriteLine($"CPU cores allowed {Environment.ProcessorCount}");
        
        Chunks = CreateChunks();
        
        /*
        using var sr = new StreamReader(file, Encoding.ASCII, false);
        while (!sr.EndOfStream)
        {
            
        }*/
        return null;
    }

   

    private static Chunk[] CreateChunks()
    {
        var chunks = new Chunk[Environment.ProcessorCount];
        int chunkSize = GetChunkSize();
        Console.WriteLine($"Chunk size = {chunkSize/1024/1024} mb");
        for (int i = 0; i < chunks.Length; i++)
        {
            chunks[i] = new Chunk()
            {
                Line = GC.AllocateUninitializedArray<byte>(chunkSize, true)
            };
        }
        return chunks;
    }

    private static int GetChunkSize()
    {
        const double memoryUsageForChunksCof = 0.90d;
        Console.WriteLine($"Use - {memoryUsageForChunksCof * 100} % memory for chunks");
        long memoryForChunks = (long)(PcMemory.FreeMemoryBytes * memoryUsageForChunksCof);
        long chunkSizeByProc = memoryForChunks / Environment.ProcessorCount;
        if (chunkSizeByProc <= int.MaxValue) return (int)chunkSizeByProc;
        return int.MaxValue;
    }
}