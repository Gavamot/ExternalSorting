using System.Buffers;
using System.Diagnostics;

using ExternalSorting;

namespace Domain;


public record ChunkForWrite(string Path, int Length);


public class ChunkProducerConfig
{
    public string Input { get; set; } = "./input.txt";
    public string ChunkFolder { get; set; } = "./chunks";
    public int ChunkSize { get; set; } = 1024 * 1024 * 1024;
    public int MaxProduce { get; set; } = Environment.ProcessorCount * 2;
}

public class ChunkProducer
{
    public ChunkProducer(string input)
    {
        this.input = input;
        this.chunkSize = GetChunkSize();
        this.chunkWriter = new ChunkWriter(Environment.ProcessorCount);
    }
    
    /// <summary>
    ///  For debug
    /// </summary>
    public ChunkProducer(ChunkProducerConfig config)
    {
        this.input = config.Input;
        this.chunkSize = config.ChunkSize;
        this.chunksFolder = config.ChunkFolder;
        this.chunkWriter = new ChunkWriter(config.MaxProduce);
    }
    
    private readonly int chunkSize; 
    private readonly string input;
    private readonly ChunkWriter chunkWriter;

    private int number = 1;
    private readonly string chunksFolder;
    string ChunkName => $"{chunksFolder}/chunk_{number++}.txt";

    private async Task CreateChunkDirectory()
    {
        try
        {
            if(Directory.Exists(chunksFolder)) Directory.Delete(chunksFolder, true);
            Directory.CreateDirectory(chunksFolder);
            // Sometimes it does not have time to create
            while (!Directory.Exists(chunksFolder))
            {
                await Task.Delay(1);
                Directory.CreateDirectory(chunksFolder);
            }
        }
        catch (Exception e)
        {
            throw new AppException($"Problems connected with work folder creation {chunksFolder} There may be problems with the operation of the program. Try to reboot your pc and run this program with administrator privileges", e);
        }
    }
    
    public async Task<ChunkForWrite[]> CreateChunks()
    {
        await CreateChunkDirectory();
        List<Chunk> chunks = new();
        await using FileStream fr = new(input, FileMode.Open, FileAccess.Read);
        
        int bufSize = chunkSize - Global.MaxStringLength;
        var buf1 = ArrayPool<byte>.Shared.Rent(chunkSize); // https://adamsitnik.com/Array-Pool/
        int buf1Red = await fr.ReadAsync(buf1, 0, bufSize);
        
        Chunk chunk = new()
        {
            Line = buf1,
            Start = 0,
            Path = ChunkName
        };
        chunks.Add(chunk);
        
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
            chunks.Add(chunk);
            
            buf1 = buf2;
            buf1Red = buf2Red;
        }
        
        chunk.End = buf1Red + startNextChunk - 1;

        if (chunk.Length >= Global.MinStringLength) chunkWriter.SortChunkAndWriteToDisk(chunk);
        else chunks.Remove(chunk);
        
        await chunkWriter.WaitWhenAllTaskCompleted();
        var res = chunks.Select(x=> new ChunkForWrite(x.Path, x.Length)).ToArray();
        return res;
    }
    
    private static int GetChunkSize()
    {
        const double memoryUsageForChunksCof = 0.90d;
        long memoryForChunks = (long)(PcMemory.FreeMemoryBytes * memoryUsageForChunksCof);
        long chunkSizeByProc = memoryForChunks / Environment.ProcessorCount;
        if (chunkSizeByProc <= int.MaxValue) return (int)chunkSizeByProc;
        return int.MaxValue;
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
    
    public StringLine[] GetSortedLines() // public for test
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