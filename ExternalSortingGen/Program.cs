

using System.Diagnostics;
using Domain;
using ExternalSortingGen;

const ulong count = 1_000_000_000;
var allowsRows = PcMemory.FreeMemoryBytes / 550d;
if (allowsRows < 100_000)
{
    Console.Error.WriteLine("Not enough free RAM for operation");
    return;
}

const int maxSize = 20_000_000;
var butchSize = allowsRows > maxSize ? maxSize : allowsRows;

Console.WriteLine($"START GENERATION count = {count} - butch size = {(int)butchSize}");
var sw = Stopwatch.StartNew();
Generator.WriteToFile("./input.txt", count, (int)butchSize);
sw.Stop();
Console.WriteLine($"DONE - spend {sw.ElapsedMilliseconds} ms");