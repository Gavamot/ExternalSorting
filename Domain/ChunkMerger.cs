using System.Buffers;

namespace Domain;

public class ChunkMergerConfig
{
    public int MinForSizeBytes { get; set; } = 1048576; // 1mb;
    public int BufferFoWriteBytes { get; set; } = 104_857_600; // 100mb;
}

public class ChunkMerger
{
    private readonly ChunkMergerConfig config;

    public ChunkMerger(ChunkMergerConfig config)
    {
        this.config = config;
    }
    private int GetChunkBuffer(ChunkForWrite[] chunks)
    {
        long free = PcMemory.FreeMemoryBytes;
        long freeMemory = (free - config.BufferFoWriteBytes) / 2;
        int maxChunkBuffer = chunks.Max(x => x.Length);
        long chunkBuffer =freeMemory / chunks.Length;
        if (chunkBuffer > maxChunkBuffer) chunkBuffer = maxChunkBuffer;
        var res = BinaryMath.ToNearestPow2((int)chunkBuffer);
        int chunkTotalCostBytes = res * 2;
        Console.WriteLine($"Free {free.BytesToMbs()} mb. Chunks {chunks.Length}, each chunk need {chunkTotalCostBytes} bytes include bufferSize {res}");
        if (chunkTotalCostBytes < config.MinForSizeBytes) throw new AppException($"You have only {chunkTotalCostBytes} bytes for each chunk but min value is {config.MinForSizeBytes} bytes");
        return res;
    }

    private void PrepareFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                Console.WriteLine($"output file ({path}) is already exist. It will be removed");
                File.Delete(path);
            }
        }
        catch (IOException e)
        {
            throw new AppException($"output file {path} already exist. Cam nor remove it", e);
        }
    }
    
    public async Task MergeChunks(string path, ChunkForWrite[] chunks)
    {
        PrepareFile(path);
        byte[] buf = ArrayPool<byte>.Shared.Rent(config.BufferFoWriteBytes);
        int chunkBufSizeBytes = GetChunkBuffer(chunks);
        using var chunkReaders = new ChunkReadersCollection(chunks, chunkBufSizeBytes);
        int maxWriteBytes = buf.Length - Global.MaxStringLength;
        await chunkReaders.SortChunksAsync(path, buf, maxWriteBytes);
        ArrayPool<byte>.Shared.Return(buf);
        DeleteChunks(chunks);
    }
    
    private void DeleteChunks(ChunkForWrite[] chunks)
    {
        try
        {
            var dir = new FileInfo(chunks.First().Path).Directory?.FullName;
            Directory.Delete(dir, true);
        }
        catch (IOException e)
        {
            Console.WriteLine("Can not remove chunks directory");
        }
    }
    
    class ChunkReadersCollection : IDisposable
    {
        private readonly ChunkForWrite[] chunkForWrites;
        private readonly ChunkReader[] chunkReaders;
        public ChunkReadersCollection(ChunkForWrite[] chunkForWrites, int chunkBufferSize)
        {
            this.chunkForWrites = chunkForWrites;
            chunkReaders = chunkForWrites.Select(x => new ChunkReader(x, chunkBufferSize)).ToArray();
        }
        
        public async Task SortChunksAsync(string path, byte[] buf, int maxWriteBytes)
        {
            var readTasks = chunkReaders.Select(x=> x.ReadLines().GetAsyncEnumerator()).ToArray();
            await Task.WhenAll(readTasks.Select(async x =>  await x.MoveNextAsync()).ToArray());
            var lines = readTasks.Select(x => x.Current).ToArray();

            int cur = 0;
            await using FileStream sw = File.OpenWrite(path);
            while (true)
            {
                (int index, var str) = GetSortedString(lines);
                
                if (str == null)
                {
                    sw.Write(buf, 0, cur);
                    sw.Flush(); 
                    break;
                }

                var ln = str.GetLength();
                Array.Copy(str.line, str.start, buf, cur, ln);
                cur += ln;
                if (cur >= maxWriteBytes)
                {
                    sw.Write(buf, 0, cur);
                    sw.Flush();
                    cur = 0;
                }

                await readTasks[index].MoveNextAsync();
                lines[index] = readTasks[index]?.Current == null ? null :  readTasks[index].Current;
            }

           
        }

        private (int, StringLine? res) GetSortedString(StringLine?[] lines)
        {
            int maxIndex = 0;
            StringLine? res = null;
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (res == null)
                {
                    maxIndex = i;
                    res = line;
                }
                else if(lines[i] != null)
                {
                    if (res.CompareTo(line!) == StringLine.More) continue;
                    maxIndex = i;
                    res = line;
                }
            }
            if (res == null) return (-1, null);
            return (maxIndex, res);
        }
        
        public void Dispose()
        {
            foreach (var chunkReader in chunkReaders)
            {
                chunkReader.Dispose();
            }

            try
            {
                var dir = new FileInfo(chunkForWrites.First().Path).Directory ?? throw new IOException("Can not find chunks directory");
                Directory.Delete(dir.FullName, true);
            }
            catch (Exception e)
            {
                Console.WriteLine($"! Can not find chunks directory cause {e.Message}");
            }
        }
    }
    
    class ChunkReader : IDisposable
    {
        readonly ChunkForWrite chunk;
        readonly byte[] buf;
        public ChunkReader(ChunkForWrite chunk, int bufSizeBytes)
        {
            this.chunk = chunk;
            buf = ArrayPool<byte>.Shared.Rent(bufSizeBytes);
        }

        public async IAsyncEnumerable<StringLine?> ReadLines()
        {
            await using FileStream fr = new(chunk.Path, FileMode.Open, FileAccess.Read);
            int redBytes = 0;
            byte[] endOfString = new byte[Global.MaxStringLength];
            int endOfStringLength = 0;
            int readBytes = buf.Length - endOfString.Length;
            int start = 0;
            StringLine lastString;
            while ((redBytes = await fr.ReadAsync(buf, endOfStringLength, readBytes)) > 0)
            {
                if(endOfStringLength > 0) 
                    Array.Copy(endOfString, buf, endOfStringLength);
                start = endOfStringLength;
                var packEnd = start + redBytes;
                start = 0;
                for (int i = start; i < packEnd; i++)
                {
                    if (buf[i] != AsciiCodes.LineEnd) continue;
                    lastString = new StringLine(buf, start, i);
                    yield return lastString;
                    start = i + 1;
                }

                endOfStringLength = packEnd - start;
                if (endOfStringLength > 0) Array.Copy(buf, start, endOfString, 0, endOfStringLength);
            }
            
            yield return null;
        }

        public void Dispose()
        {
            ArrayPool<byte>.Shared.Return(buf);
        }
    }
}