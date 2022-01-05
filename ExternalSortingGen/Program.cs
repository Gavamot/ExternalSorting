using System.Diagnostics;
using CommandLine;
using Domain;
using ExternalSortingGen;

try
{
    Parser.Default.ParseArguments<Options>(args).WithParsed(options =>
    {
        AppFile.MustRemove(options.Output);
        if (string.IsNullOrWhiteSpace(options.Test))
        {
            var allowsRows = PcMemory.FreeMemoryBytes / 512;
            if (allowsRows < 100_000)
            {
                Console.Error.WriteLine("Not enough free RAM for operation");
                return;
            }
            const int maxSize = 20_000_000;
            var butchSize = allowsRows > maxSize ? maxSize : allowsRows;
        
            Console.WriteLine($"START SIMPLE GENERATION  count = {options.Count} - butch size = {butchSize}");
            var sw = Stopwatch.StartNew();
            Generator.WriteToFile(options.Output, options.Count, (int)butchSize, options.Seed);
            sw.Stop();
            Console.WriteLine($"DONE - spend {sw.ElapsedMilliseconds} ms");
        }
        else
        {
            Console.WriteLine($"START TEST GENERATION words count = {options.Count}");
            var sw = Stopwatch.StartNew();
            Generator.WriteTestToFile(options.Output, options.Test, (int)options.Count, options.Seed);
            sw.Stop();
            Console.WriteLine($"DONE - spend {sw.ElapsedMilliseconds} ms");
        }
    }).WithNotParsed(errs => Console.Error.WriteLine("Arguments are wrong"));
}
catch(Exception e)
{
    Console.Error.WriteLine(e.Message);
}
class Options
{
    [Option('c', "count", Required = true, HelpText = "Rows  for simple mode. Count of words for test mode")]
    public long Count { get; set; }
    
    [Option('o', "output", Required = false, Default = "./input.txt", HelpText = "Output file")]
    public string Output { get; set; }

    [Option('t', "test", Required = false, Default = "", HelpText = "Path to correct soft generation. If is not empty Test mode else simple mode")]
    public string Test { get; set; }

    [Option('s', "seed", Required = false, HelpText = "Seed for random. Only for file without test")]
    public int Seed { get; set; } = 123;
}