using System.Buffers;
using System.Collections.Concurrent;

namespace Domain;

public class ChunkMerger
{
    public static readonly ArrayPool<byte> bufferPool = ArrayPool<byte>.Create(); // Very bad spagety code
    
    private ConcurrentQueue<ChunkForWrite> chunksQueue;
    private volatile int produce = 0;
    private int maxProduce = Environment.ProcessorCount;
    private int restOfChunks = 0;
    
    public async Task MergeChunksAsync(string path, string chunksFolder, ChunkForWrite[] allChunks)
    {
        restOfChunks = allChunks.Length;
        BigChunkNameGenerator chunkNameGenerator = new(chunksFolder);
        AppFile.MustRemove(path);
        long totalSizeBytes = allChunks.Sum(x => x.Length);
        chunksQueue = new ConcurrentQueue<ChunkForWrite>(allChunks);
        var mergeThreads = maxProduce > allChunks.Length ? allChunks.Length : maxProduce; 
        int chunkBufSizeBytes = GetChunkBufferSize(mergeThreads);
        
        ChunkForWrite chunk1 = null!;
        while (maxProduce > 0)
        {
            while (produce >= maxProduce)
            {
                await Task.Delay(1);
            }

            if (restOfChunks < maxProduce)
            {
                chunkBufSizeBytes = GetChunkBufferSize(restOfChunks);
                maxProduce--;
            }
            
            chunk1 = await GetChunkAsync();
            if (chunk1.Length >= totalSizeBytes) break;
            ChunkForWrite chunk2 = await GetChunkAsync();
            var chunks = new[] {chunk1, chunk2};
            
            Interlocked.Increment(ref produce);
            restOfChunks--;

            var bytes = chunkBufSizeBytes;
            Task.Factory.StartNew(() =>
            {
                try
                {
                    var bigChunkName = chunkNameGenerator.GetName();    
                    var bigChunk = new ChunkReadersCollection().SortChunks(bigChunkName, chunks, bytes);
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

    private async Task<ChunkForWrite> GetChunkAsync()
    {
        ChunkForWrite chunk;
        while (!chunksQueue.TryDequeue(out chunk))
        {
            await Task.Delay(1);
        }
        return chunk;
    }
    
    private int GetChunkBufferSize(int mergersCount)
    {
        GC.Collect(2, GCCollectionMode.Forced, true, true);
        long free = PcMemory.FreeMemoryBytes;
        const int permanentCostBytes = 3_048_576;
        const int bufferCount = 3; // 1 - chank 1 + chank 2 + writeBuffer;
        double chunkBufferSize = ( (free - (permanentCostBytes * mergersCount)) / (mergersCount * bufferCount));
        if (chunkBufferSize > int.MaxValue) return BinaryMath.ToNearestPow2(int.MaxValue);
        if (chunkBufferSize > int.MaxValue) return int.MaxValue;
        return (int)chunkBufferSize;
    }
    
    
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
        public ChunkForWrite SortChunks(string path, ChunkForWrite[] chunks, int bufferSizeBytes)
        {
            int maxWriteBytes = bufferSizeBytes - Global.MaxStringLength;
            byte[] writeBuffer = bufferPool.Rent(bufferSizeBytes);
            var chunkReaders = chunks.Select(x => new ChunkReader(x, bufferSizeBytes)).ToArray();
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