using Domain;

//var maxProduce = Environment.ProcessorCount * 2;
//var chunkSizeBytes = 1024 * 1024 * 100;
var str = Environment.NewLine;
Console.WriteLine((byte)str[0]);
Console.WriteLine((byte)str[1]);
var chunkProducer = new ChunkProducer("./input.txt", 256, 1);
var res = await chunkProducer.CreateChunks();  


Console.WriteLine("IT IS DONE");