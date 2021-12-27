using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System;

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
        this.maxProduce = maxProduce;
    }

    private readonly int chunkSize; 
    private readonly string sourcePath;
    private readonly int maxProduce;
    
    private volatile int produce;
    
    const int AsciiStringLineEnd = 10;

    private int number = 1;
    string ChunkName => $"./chunks/chunk_{number++}.txt";

    public void Start() => Task.Factory.StartNew(StartAsync, TaskCreationOptions.LongRunning).Start();
    
    async Task StartAsync()
    {
        var file = new FileInfo(sourcePath);
        var maxLength = file.Length;

        await using FileStream fr = new(sourcePath, FileMode.Open, FileAccess.Read);
        
        const int maxStringLength = 128;
        int bufSize = chunkSize - maxStringLength;
        int lastBufferIndex = bufSize - 1; 
        
        var buf1 = ArrayPool<byte>.Shared.Rent(chunkSize);
        int cur = 0;
        await fr.ReadAsync(buf1.AsMemory(cur, bufSize));
        
        Chunk chunk = new()
        {
            Line = buf1,
            Start = cur,
            Path = ChunkName
        };
        
        cur += bufSize;
        
        int startNextChunk = 0;
        int haveRed = 0;
        while (cur < maxLength)
        {
            while (produce >= maxProduce)
            {
                await Task.Delay(1);
            }
            
            var buf2 = ArrayPool<byte>.Shared.Rent(chunkSize);
            haveRed = await fr.ReadAsync(buf2.AsMemory(cur, bufSize));
            
            if (buf1[lastBufferIndex] != AsciiStringLineEnd)
            {
                // Complete our Chunk copy packet end to start
                while (startNextChunk < haveRed)
                {
                    if (buf2[startNextChunk] == AsciiStringLineEnd)
                    {
                        Array.Copy(buf2, 0, buf1, bufSize, startNextChunk + 1);
                        break;
                    }
                    startNextChunk++;
                }
            }
            
            chunk.End = haveRed + startNextChunk - 1;
            // Execute task on separate thread
            
            SortChunkAndWriteToDisk(chunk);

            chunk = new Chunk()
            {
                Start = startNextChunk,
                Line = buf2,
                Path = ChunkName
            };
            
            cur += bufSize;
            buf1 = buf2;
        }
        
        chunk.End = haveRed + startNextChunk - 1;
        SortChunkAndWriteToDisk(chunk);
    }
    
    async Task InnerSortChunkAndWriteToDisk(object? chunkObj)
    {
        var chunk = chunkObj as Chunk ?? throw new ArgumentException("Chunk can not be null");
        Interlocked.Increment(ref produce);
        chunk.SortLine();
        await chunk.WriteToDiskAsync();
        chunk.Dispose();
        Interlocked.Decrement(ref produce);
    }
    
    void SortChunkAndWriteToDisk(Chunk chunc) =>
        Task.Factory.StartNew(InnerSortChunkAndWriteToDisk, chunc, TaskCreationOptions.LongRunning).Start();
}

public class ExternalSorter
{
    public void Sort(string path, int chunkSize)
    {
        int chunksCount = Environment.ProcessorCount;
        var fileInfo = new FileInfo(path);
        // ! Проверить на наличие

        /*using (FileStream fsSource = new FileStream(pathSource, FileMode.Open, FileAccess.Read))
        {
            
        }*/
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
        ArrayPool<byte>.Shared.Return(Line);
    }
}

public class ChunkLineWriter
{
    public static byte[] writeBuffer;
    public static byte[] writeToFileBuffer;
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