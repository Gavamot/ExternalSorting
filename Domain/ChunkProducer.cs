using System.Buffers;
using System.Diagnostics;
using System;
using System.Text;
using ExternalSorting;

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

    private int number = 1;
    private const string ChunksFolder = "./chunks";
    string ChunkName => $"{ChunksFolder}/chunk_{number++}.txt";

    private async Task CreateChunkDirectory()
    {
        try
        {
            if(Directory.Exists(ChunksFolder)) Directory.Delete(ChunksFolder, true);
            Directory.CreateDirectory(ChunksFolder);
            // Sometimes it does not have time to create
            while (!Directory.Exists(ChunksFolder))
            {
                await Task.Delay(1);
                Directory.CreateDirectory(ChunksFolder);
            }
        }
        catch (Exception e)
        {
            string error = $"Problems connected with work folder creation {ChunksFolder} There may be problems with the operation of the program. Try to reboot your pc and run this program with administrator privileges";
            await Console.Error.WriteLineAsync(error);
            throw new Exception(error);
        }
    }
    
    public async Task<string[]> CreateChunks()
    {
        await CreateChunkDirectory();
        List<Chunk> res = new();
        await using FileStream fr = new(sourcePath, FileMode.Open, FileAccess.Read);
        
        int bufSize = chunkSize - Global.MaxStringLength;
        var buf1 = ArrayPool<byte>.Shared.Rent(chunkSize); // https://adamsitnik.com/Array-Pool/
        int buf1Red = await fr.ReadAsync(buf1, 0, bufSize);
        
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
            var buf2Red = await fr.ReadAsync(buf2.AsMemory(0, bufSize));
            
            startNextChunk = 0;
            if (buf1[buf1Red - 1] != AsciiCodes.LineEnd)
            {
                // Complete our Chunk copy packet end to start
                while (startNextChunk < buf2Red)
                {
                    if (buf2[startNextChunk++] == AsciiCodes.LineEnd)
                    {
                        Array.Copy(buf2, 0, buf1, buf1Red, startNextChunk);
                        break;
                    }
                }
            }

            chunk.End = buf1Red + startNextChunk - 1;
            
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

        if (chunk.Length >= Global.MinStringLength) chunkWriter.SortChunkAndWriteToDisk(chunk);
        else res.Remove(chunk);
        
        await chunkWriter.WaitWhenAllTaskCompleted();
        return res.Select(x=>x.Path).ToArray();
    }
    
    private class ChunkWriter
    {
        public ChunkWriter(int maxProduce)
        {
            this.maxProduce = maxProduce;
        }
        
        private readonly int maxProduce;
        private volatile int produce;
        
        private List<Task> Tasks = new();
        public void SortChunkAndWriteToDisk(Chunk chunk)
        {
            Interlocked.Increment(ref produce);
            Task t = Task.Factory.StartNew( () =>
            {
                var sw = Stopwatch.StartNew();
                chunk.SortAndWriteToDisk(); // Blocking operation but for this task it is OK. Because sync works faster in this case
                chunk.Dispose();
                sw.Stop();
                Console.WriteLine($"{chunk.Path} had been created - {sw.ElapsedMilliseconds} ms");
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
    
    StringLine[] GetSortedLines()
    {
        var size = Length / Global.MidStringLength;
        List<StringLine> lines = new(size); // ? I can pool it if need faster
        int lineStart = Start;
        
        for (int i = Start; i <= End; i++)
        {
            if (Line[i] != AsciiCodes.LineEnd) continue;
            lines.Add(new(Line, lineStart, i));
            lineStart = i + 1;
        }

        var res = lines.ToArray();
        Sorting.QuickSort.Sort(res);
        return res;
    }

    byte[] SortedLinesToByteArray(StringLine[] lines)
    {
        var writeBuffer = ArrayPool<byte>.Shared.Rent(Length);
        int insertIndex = 0;
        for (int i = 0; i < lines.Length; i++)
        {
            int ln = lines[i].GetLength();
            Array.Copy(Line, lines[i].start, writeBuffer, insertIndex, ln);
            insertIndex += ln;
        }
        return writeBuffer;
    }
    
    public void SortAndWriteToDisk()
    {
        var lines = GetSortedLines();
        var writeBuffer = SortedLinesToByteArray(lines);
        using var stream = File.OpenWrite(Path);
        stream.Write(writeBuffer, 0, Length);
        stream.Flush();
        ArrayPool<byte>.Shared.Return(writeBuffer, false);
    }
    
    
    public void Dispose()
    {
        ArrayPool<byte>.Shared.Return(Line, false);
        Line = null!;
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