using BenchmarkDotNet.Running;
using ExternalSorting.Benchmark;
using ExternalSorting.Test;

//  dotnet run -c Release -f net6.0 --filter *
//BenchmarkRunner.Run<StringLineBenchmark>();
BenchmarkRunner.Run<HeapSortBenchmark>();