using BenchmarkDotNet.Running;
using ExternalSorting.Test;

var summary = BenchmarkRunner.Run<StringLineBenchmark>();