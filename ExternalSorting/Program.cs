using System.Diagnostics;
using CommandLine;
using Domain;


try
{
    Options options = null;
    Parser.Default.ParseArguments<Options>(args)
        .WithParsed(opt => options = opt).WithNotParsed(errs => Console.Error.WriteLine("Arguments are wrong"));
    if (options == null) return;
    
    var t = Stopwatch.StartNew();
    var (output, config) = options.GetSettings();
    config.Input = @"C:\dev\ExternalSorting\ExternalSortingGen\bin\Release\net6.0\publish\input.txt";
    Console.WriteLine($"Start sorting process from {config.Input} to {output} (chunks dir {config.ChunkFolder} | chunk size {config.ChunkSize} bytes), max-produce {config.MaxProduce}");
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

class Options
{
    [Option('i', "input", Required = false, HelpText = "input file")]
    public string Input { get; set; }
    
    [Option('o', "output", Required = false, HelpText = "output file")]
    public string Output { get; set; } 
    
    [Option('c', "chunks", Required = false, HelpText = "chunks folder")]
    public string ChunkFolder { get; set; }
    
    [Option('s', "size", Required = false, HelpText = "chunks size")]
    public int ChunkSize { get; set; }
    
    [Option('m', "max", Required = false, HelpText = "max threads for chunks creation")]
    public int MaxProduce { get; set; }

    public (string output, ChunkProducerConfig chunkProducerConfig) GetSettings()
    {
        var output = string.IsNullOrWhiteSpace(this.Output) ? "output.txt" : this.Output;
        var config = new ChunkProducerConfig();
        config.Input = string.IsNullOrWhiteSpace(this.Input) ?  config.Input: this.Input;
        config.ChunkFolder = string.IsNullOrWhiteSpace(this.ChunkFolder) ?  config.ChunkFolder: this.ChunkFolder;
        config.ChunkSize = this.ChunkSize == 0 ? config.ChunkSize : this.ChunkSize;
        config.MaxProduce = this.MaxProduce == 0 ? config.MaxProduce: this.MaxProduce;
        return (output, config);
    }
}