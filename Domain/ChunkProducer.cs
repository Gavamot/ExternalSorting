using System.Buffers;

using ExternalSorting;

namespace Domain;
public record ChunkForWrite(string Path, long Length);
public class ChunkProducerConfig
{
    public string Input { get; set; } = "./input.txt";
    public string ChunkFolder { get; set; } = "./chunks";
    public int ChunkSize { get; set; } = GetChunkSize();
    public int MaxProduce { get; set; } = Environment.ProcessorCount;
    public const int OptimalChunkSizeMb = 64;
    const double MemoryUsageForChunksCof = 0.80d;
    private static int GetChunkSize()
    {
        long memoryForChunks = (long)(PcMemory.FreeMemoryBytes * MemoryUsageForChunksCof);
        long chunkSizeByProc = memoryForChunks / Environment.ProcessorCount;
        if (chunkSizeByProc.BytesToMbs() > OptimalChunkSizeMb) return (int) OptimalChunkSizeMb.MbsToBytes();
        Console.WriteLine($"Your Pc has not enough memory. For normal works need {OptimalChunkSizeMb * Environment.ProcessorCount}");
        return (int) chunkSizeByProc;
    }
}

public class ChunkProducer
{
    public readonly ArrayPool<byte> bufferPool = ArrayPool<byte>.Create(); // Very bad spagety code
    
    public ChunkProducer(ChunkProducerConfig config)
    {
        this.input = config.Input;
        this.chunkSize = config.ChunkSize;
        this.chunksFolder = config.ChunkFolder;
        this.chunkWriter = new ChunkWriter(bufferPool, config.MaxProduce);
    }
    
    private readonly int chunkSize; 
    private readonly string input;
    private readonly ChunkWriter chunkWriter;

    private int number = 1;
    private readonly string chunksFolder;
    string GetChunkName() => $"{chunksFolder}/chunk_{number++}.txt";

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
        var buf1 = bufferPool.Rent(chunkSize); // https://adamsitnik.com/Array-Pool/
        int buf1Red = fr.Read(buf1, 0, bufSize);
        
        Chunk chunk = new()
        {
            Line = buf1,
            Start = 0,
            Path = GetChunkName()
        };
        chunks.Add(chunk);
        
        int startNextChunk = 0;
        
        while (buf1Red > 0)
        {
            await chunkWriter.WaitProduce();
            
            var buf2 = bufferPool.Rent(chunkSize);
            var buf2Red = fr.Read(buf2,0, bufSize);
            
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
                Path = GetChunkName()
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
    
    private class ChunkWriter
    {
        public ChunkWriter(ArrayPool<byte> bufferPool, int maxProduce)
        {
            this.bufferPool = bufferPool;
            this.maxProduce = maxProduce;
        }

        private readonly ArrayPool<byte> bufferPool;
        private readonly int maxProduce;
        private volatile int produce;
        
        private readonly List<Task> Tasks = new();
        public void SortChunkAndWriteToDisk(Chunk chunk)
        {
            Interlocked.Increment(ref produce);
            var writeBuffer = bufferPool.Rent(chunk.Length);
            Task t = Task.Factory.StartNew( () =>
            {
                chunk.SortAndWriteToDisk(writeBuffer); // Blocking operation but for this task it is OK. Because sync works faster in this case
                bufferPool.Return(writeBuffer, false);
                bufferPool.Return(chunk.Line, false);
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
    
    public class Chunk
    {
        public byte[] Line { get; set; }
        public int Start { get; set; }
        public int End { get; set; }
        public int Length => End - Start + 1;
        public string Path { get; set; }
    
        public StringLine[] GetSortedLines() // public for test
        {
            try
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
                Sorting.HeapSort(res);
                return res;
            }
            catch(Exception e)
            {
                return null;
            }
        }

        void SortedLinesToByteArray(StringLine[] lines, byte[] writeBuffer)
        {
            int insertIndex = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                int ln = lines[i].GetLength();
                Array.Copy(Line, lines[i].start, writeBuffer, insertIndex, ln);
                insertIndex += ln;
            }
        }
    
        public void SortAndWriteToDisk(byte[] writeBuffer)
        {
            var lines = GetSortedLines();
            SortedLinesToByteArray(lines, writeBuffer);
            using var stream = File.OpenWrite(Path);
            stream.Write(writeBuffer, 0, Length);
            stream.Flush();
        }
    }
}