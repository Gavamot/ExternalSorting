using System.Buffers;
using System.Collections.Concurrent;

namespace Domain;

public static class MemoryAllocator
{
    private static int ProcessorCount = Environment.ProcessorCount;

    public static int GetChunkBufferSize(int chunksCount)
    {
        GC.Collect(2, GCCollectionMode.Forced, true, true);
        var mergersCount = chunksCount > ProcessorCount ? ProcessorCount : chunksCount;
        long free = PcMemory.FreeMemoryBytes;
        const int permanentCostBytes = 1_048_576;
        const int bufferCount = 2; // writeBuffer + chunk1
        double chunkBufferSize = (free / (mergersCount * bufferCount)) - (permanentCostBytes * mergersCount);
        if (chunkBufferSize > int.MaxValue) return int.MaxValue;
        return (int)chunkBufferSize;
    }
}

public class ChunkMerger
{
    public static readonly ArrayPool<byte> bufferPool = ArrayPool<byte>.Create(); // Very bad spagety code
    
    public async Task MergeChunksAsync(string path, string chunksFolder, ChunkForWrite[] allChunks)
    {
        BigChunkNameGenerator chunkNameGenerator = new(chunksFolder);
        AppFile.MustRemove(path);
        long totalSizeBytes = allChunks.Sum(x => x.Length);
        chunksQueue = new ConcurrentQueue<ChunkForWrite>(allChunks);
        int chunkBufSizeBytes = MemoryAllocator.GetChunkBufferSize(allChunks.Length);
        
        ChunkForWrite chunk1;
        while (true)
        {
            chunk1 = await GetChunkAsync();
            if (chunk1.Length >= totalSizeBytes) break;
            
            if (chunk1.Length > (chunkBufSizeBytes * 6L))
            {
                try
                {
                    checked
                    {
                        chunkBufSizeBytes *= 2;
                        maxProduce /= 2;
                    }
                }
                catch(Exception e)
                {
                   await Console.Error.WriteLineAsync("Overflow int buffers");
                }
            }
            
            while (produce >= maxProduce)
            {
                await Task.Delay(1);
            }
            
           var (chunksBytes, chunks) = await GetBigChunkAsync(chunk1, chunkBufSizeBytes, totalSizeBytes);
           Interlocked.Increment(ref produce);
           var bufferWriteBytes = chunkBufSizeBytes;
           Task.Factory.StartNew(() =>
            {
                try
                {
                    var bigChunkName = chunkNameGenerator.GetName();    
                    var bigChunk = new ChunkReadersCollection().SortChunks(bigChunkName, chunks, bufferWriteBytes, chunksBytes);
                    chunksQueue.Enqueue(bigChunk);
                    Interlocked.Decrement(ref produce);
                }
                catch(Exception e)
                {
                    Console.Error.WriteLine(e.Message);
                    AppFile.TryRemoveFolder(chunksFolder);
                    Environment.Exit(1);
                }
            }, TaskCreationOptions.LongRunning);
        }
        
        File.Move(chunk1.Path, path);
        AppFile.TryRemoveFolder(chunksFolder);
    }

    private async Task<(int bufferSize, List<ChunkForWrite>)> GetBigChunkAsync(ChunkForWrite chunk1, int bufferSizeBytes, long totalBytes)
    {
        List<ChunkForWrite> chunks = new(){ chunk1 };
        long cur = chunk1.Length;
        while (cur < totalBytes)
        {
            var chunk = await GetChunkAsync();
            if (cur + chunk.Length >= bufferSizeBytes)
            {
                chunksQueue.Enqueue(chunk);
                break;
            }
            chunks.Add(chunk);
            cur += chunk.Length;
        }
        return ((int)(cur / chunks.Count), chunks);
    }
    
    
    private async Task<ChunkForWrite> GetChunkAsync()
    {
        ChunkForWrite chunk;
        while (!chunksQueue.TryDequeue(out chunk))
        {
            await Task.Delay(1);
        }
        return chunk;
    }
    
    
    private ConcurrentQueue<ChunkForWrite> chunksQueue;
    private volatile int produce = 0;
    private int maxProduce = Environment.ProcessorCount;
    
    class BigChunkNameGenerator
    {
        private int chunkNum = 0;
        private readonly string startOfName;
        
        public BigChunkNameGenerator(string chunksFolder)
        {
            this.startOfName = $"{chunksFolder}/bigChunk";
        }

        public string GetName()
        {
            int num = Interlocked.Increment(ref chunkNum);
            return $"{startOfName}_{num}.txt";
        }
    }
    
    class ChunkReadersCollection
    {
        public ChunkForWrite SortChunks(string path, List<ChunkForWrite> chunks, int bufferWriteBytes, int bufferReadChunkBytes)
        {
            int maxWriteBytes = bufferWriteBytes - Global.MaxStringLength;
            byte[] writeBuffer = bufferPool.Rent(bufferWriteBytes);
            var chunkReaders = chunks.Select(x => new ChunkReader(x, bufferReadChunkBytes)).ToArray();
            IEnumerator<StringLine?>[] readTasks = chunkReaders.Select(x=> x.ReadLines()).ToArray();
            readTasks.Foreach(x=> x.MoveNext());
            var lines = readTasks.Select(x => x.Current).ToArray();

            int cur = 0;
            using FileStream sw = File.OpenWrite(path);
            while (true)
            {
                (int index, var str) = GetSortedString(lines);
                
                if (str == null)
                {
                    sw.Write(writeBuffer, 0, cur);
                    sw.Flush(); 
                    break;
                }

                var ln = str.GetLength();
                Array.Copy(str.line, str.start, writeBuffer, cur, ln);
                cur += ln;
                if (cur >= maxWriteBytes)
                {
                    sw.Write(writeBuffer, 0, cur);
                    sw.Flush();
                    cur = 0;
                }

                readTasks[index].MoveNext();
                lines[index] = readTasks[index]?.Current == null ? null : readTasks[index].Current;
            }
            
            sw.Close();
            sw.Dispose();
            
            chunkReaders.Foreach(x=> x.Dispose());
            return new (path,chunks.Sum(x => x.Length));
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
    }
    
    class ChunkReader : IDisposable
    {
        public readonly ChunkForWrite chunk;
        readonly byte[] buf;
        public ChunkReader(ChunkForWrite chunk, int bufSizeBytes)
        {
            this.chunk = chunk;
            buf = bufferPool.Rent(bufSizeBytes);
        }

        public IEnumerator<StringLine> ReadLines()
        {
            FileStream fr = new(chunk.Path, FileMode.Open, FileAccess.Read);
            int redBytes = 0;
            byte[] endOfString = new byte[Global.MaxStringLength];
            int endOfStringLength = 0;
            int readBytes = buf.Length - endOfString.Length;
            while ((redBytes = fr.Read(buf, endOfStringLength, readBytes)) > 0)
            {
                if(endOfStringLength > 0) Array.Copy(endOfString, buf, endOfStringLength);
                var start = endOfStringLength;
                var packEnd = start + redBytes;
                start = 0;
                for (int i = start; i < packEnd; i++)
                {
                    if (buf[i] != AsciiCodes.LineEnd) continue;
                    var lastString = new StringLine(buf, start, i);
                    yield return lastString;
                    start = i + 1;
                }

                endOfStringLength = packEnd - start;
                if (endOfStringLength > 0) Array.Copy(buf, start, endOfString, 0, endOfStringLength);
            }
            fr.Close(); // if use using throws error then dispose it!!!
            fr.Dispose();
            yield return null;
            
        }

        public void Dispose()
        {
            bufferPool.Return(buf);
            File.Delete(chunk.Path);
        }
    }
}